package main

import (
	"encoding/json"
	"fmt"
	"math/rand"
	"os/exec"
	"strconv"
	"time"

	db "gopkg.in/gorethink/gorethink.v4"
)

type matchStatus int
type matchType int
type matchSide bool
type playerStatus int
type playerLimit int

const (
	stReserved matchStatus = iota
	stReady
	stPlay
	stEnd
	stReleased
)
const (
	none matchType = iota
	mt1vs1
	mt2vs2
	mt3vs3
	mt4vs4
	mt5vs5
	mtDM
	mtBattle
)

const (
	ms1vs1   matchSide = true
	ms2vs2   matchSide = true
	ms3vs3   matchSide = true
	ms4vs4   matchSide = true
	ms5vs5   matchSide = true
	msDM     matchSide = false
	msBattle matchSide = false
)

const (
	psJoined playerStatus = iota
	psPlaying
	psLeft
	psFinished
)

const (
	pl1vs1   playerLimit = 2
	pl2vs2   playerLimit = 4
	pl3vs3   playerLimit = 6
	pl4vs4   playerLimit = 8
	pl5vs5   playerLimit = 10
	plDM     playerLimit = 0
	plBattle playerLimit = 0
)

// Sided returns true if a match type has 2-sides
func Sided(mt matchType) bool {
	var l matchSide
	if mt == mt1vs1 {
		l = ms1vs1
	} else if mt == mt2vs2 {
		l = ms2vs2
	} else if mt == mt3vs3 {
		l = ms3vs3
	} else if mt == mt4vs4 {
		l = ms4vs4
	} else if mt == mt5vs5 {
		l = ms5vs5
	} else if mt == mtDM {
		l = msDM
	} else if mt == mtBattle {
		l = msBattle
	} else {
		l = ms1vs1
	}
	return bool(l)
}

// PlayerLimit returns the player limit based on match type
func PlayerLimit(mt matchType) int {
	var l playerLimit
	if mt == mt1vs1 {
		l = pl1vs1
	} else if mt == mt2vs2 {
		l = pl2vs2
	} else if mt == mt3vs3 {
		l = pl3vs3
	} else if mt == mt4vs4 {
		l = pl4vs4
	} else if mt == mt5vs5 {
		l = pl5vs5
	} else if mt == mtDM {
		l = plDM
	} else if mt == mtBattle {
		l = plBattle
	} else {
		l = pl1vs1
	}
	return int(l)
}

const (
	errRoomFull        Err = 201
	errAlreadyJoined   Err = 202
	errJoinFailed      Err = 203
	errNoMatchFound    Err = 204
	errServerReserv    Err = 205
	errPlayerLeave     Err = 206
	errCharacterSelect Err = 207
	errConnectionSync  Err = 208
)

// Event structure
type Event struct {
	User    *Account `json:"User,omitable"`
	Match   *Match   `json:"M,omitable"`
	Players []Player `json:"Players,omitable"`
	Me      *Player  `json:"Me,omitable"`
}

// match service
func match(r *R) {
	DBCheck()

	var event Event
	if err := json.Unmarshal([]byte((*r).Request), &event); err != nil {
		(*r).Error = ErrSys
		return
	}

	// authorize user
	j, err := json.Marshal(event.User)
	if err != nil {
		(*r).Error = ErrSys
		return
	}
	var login = R{
		Action:  int(ActKey),
		Request: string(j),
	}
	j, err = json.Marshal(login)
	if err != nil {
		(*r).Error = ErrSys
		return
	}
	fmt.Printf("Authorizing a user: %s...\n", event.User.Email)
	// TODO add host
	resp := Dial("127.0.0.1", Config.ServicePort("account"), string(j))
	err = json.Unmarshal(resp, &login)
	if err != nil {
		(*r).Error = ErrHidden
		fmt.Printf("An unexpected error occured between service comm!\n")
		return
	}
	err = json.Unmarshal([]byte(login.Response), &event.User)
	if err != nil {
		(*r).Error = ErrHidden
		fmt.Printf("An unexpected error occured between service comm!\n")
		return
	}
	if event.User == nil || event.User.ID == "" {
		(*r).Error = ErrAuth
		fmt.Printf("Unauthorized!\n")
		return
	}
	fmt.Printf("Authorized: %s\n", event.User.ID)

	act := Action((*r).Action)
	if act == ActPing {
		// ping
	} else if act == ActJoin {
		find(r, &event)
	} else if act == ActPlayers {
		players(r, &event)
	} else if act == ActLeave {
		leave(r, &event)
	} else if act == ActCharacter {
		character(r, &event)
	}

	(*r).Request = "" // clear the request

	j, err = json.Marshal(event)
	if err != nil {
		(*r).Error = ErrSys
		return
	}
	(*r).Response = string(j)
}

// start a server executable
func serverStart(ip string, port int) {
	// TODO run from a sh or bat script so people can edit
	err := exec.Command(Config.Server.Path,
		"-host", "127.0.0.1", "-port", strconv.Itoa(port)).Run()
	// TODO use -batchmode
	if Error(err) {
		fmt.Println("Failed to start a server instance!")
	}
}

func create(r *R, e *Event) {
	fmt.Printf("Creating a game type: %d... ", (*e).Match.Typ)

	// find a suitable server
	// TODO there is only local server for now
	(*e).Match.IP = "127.0.0.1"

	// find an available for the server
	// TODO actually check ports
	(*e).Match.Port = 0
	occupied := true
	for occupied {
		occupied = false
		(*e).Match.Port = rand.Intn(65530-5000) + 5000

		// TOOD better filtering query
		var ms = make([]Match, 0)
		cursor, err := db.Table("match").
			Filter(db.Row.Field("port").Eq((*e).Match.Port)).Run(session)
		if Error(err) {
			(*r).Error = errJoinFailed
			return
		}
		err = cursor.All(&ms)
		if Error(err) {
			(*r).Error = errJoinFailed
			return
		}
		for _, m := range ms {
			if m.IP == (*e).Match.IP && m.Status != int(stReleased) {
				occupied = true
				break
			}
		}
	}

	result, err := db.Table("match").Insert(map[string]interface{}{
		"counter":  0,
		"creation": int32(time.Now().Unix()),
		"ip":       (*e).Match.IP,
		"port":     (*e).Match.Port,
		"typ":      (*e).Match.Typ,
		"status":   int(stReserved),
	}).RunWrite(session)
	if Error(err) || result.Errors != 0 {
		(*r).Error = errJoinFailed
		return
	}
	(*e).Match.ID = result.GeneratedKeys[0]

	fmt.Printf("A new game-%d is created: %s\n", (*e).Match.Typ, (*e).Match.ID)
}

func join(r *R, e *Event) {
	fmt.Printf("Joining a game: %s\n", (*e).Match.ID)

	// already fetched and it is in e.Match
	/*
		cursor, err := db.Table("match").Get((*e).Match.ID).Run(session)
		if Error(err) {
			(*r).Error = errJoinFailed
			return
		}
		defer cursor.Close()
		err = cursor.One(&(*e).Match)
		if Error(err) {
			(*r).Error = errJoinFailed
			return
		}
	*/

	fmt.Printf("Players in the session: %d\n", (*e).Match.Counter)

	fmt.Printf("Checking if the player is in the match...\n")
	found := false

	// TODO make a better query when you learn with this plugin
	cursor2, err := db.Table("player").
		GetAllByIndex("account_id", (*e).User.ID).Run(session)
	if Error(err) {
		fmt.Printf("Can not find player match records!\n")
		(*r).Error = errJoinFailed
		return
	}
	defer cursor2.Close()
	err = cursor2.All(&(*e).Players)
	if Error(err) {
		fmt.Printf("Can not bind player records!\n")
		(*r).Error = errJoinFailed
		return
	}
	for _, p := range (*e).Players {
		fmt.Println(p.Status)
		fmt.Println(int(psJoined))
		if p.Match != (*e).Match.ID || p.Status > int(psPlaying) {
			continue
		}
		(*e).Me = &p
		found = true
		break
	}

	if found {
		fmt.Println("The player is already in the session.")

		db.Table("player").Get((*e).Me.ID).
			Update(map[string]interface{}{
				"ip":   (*r).Addr.IP.String(),
				"port": (*r).Addr.Port,
			}).RunWrite(session)

		(*r).Error = errAlreadyJoined

		return
	}

	limit := PlayerLimit(matchType((*e).Match.Typ))
	// room full?
	if (*e).Match.Counter >= limit {
		fmt.Printf("The session is full!\n")
		(*r).Error = errRoomFull
		(*e).Match = nil
		return
	}

	(*e).Match.Counter = (*e).Match.Counter + 1

	// side
	// TODO do it with count later
	(*e).Me = &Player{}
	sidx := 0
	if Sided(matchType((*e).Match.Typ)) {
		fmt.Printf("Picking a side...\n")
		side1 := 0
		side2 := 0

		cursor3, err := db.Table("player").
			GetAllByIndex("match_id", (*e).Match.ID).Run(session)
		if Error(err) {
			(*r).Error = errJoinFailed
			return
		}
		defer cursor3.Close()
		err = cursor3.All(&(*e).Players)
		if Error(err) {
			(*r).Error = errJoinFailed
			return
		}
		for _, p := range (*e).Players {
			if p.Player == (*e).User.ID {
				(*e).Me = &p
				continue
			}
			if p.Status > int(psPlaying) {
				continue
			}

			if p.Side == 0 {
				side1 = side1 + 1
			} else {
				side2 = side2 + 1
			}
		}
		if side1 <= side2 {
			sidx = 0
		} else {
			sidx = 1
		}
		(*e).Me.Side = sidx
		fmt.Printf("Side: %d\n", sidx)
	}

	cid := time.Now().UnixNano() - rand.Int63() // TODO test

	_, err = db.Table("player").Insert(map[string]interface{}{
		"account_id":    (*e).User.ID,
		"match_id":      (*e).Match.ID,
		"character":     1,
		"ip":            (*r).Addr.IP.String(),
		"port":          (*r).Addr.Port,
		"playername":    (*e).User.Nickname,
		"side":          sidx,
		"status":        0,
		"connection_id": cid,
	}).RunWrite(session)
	if Error(err) {
		(*r).Error = errJoinFailed
		return
	}

	// game ready
	status := stReserved
	if (*e).Match.Counter == limit {
		status = stReady
	}

	db.Table("match").Get((*e).Match.ID).
		Update(map[string]interface{}{
			"counter": (*e).Match.Counter,
			"status":  status,
		}).RunWrite(session)

	db.Table("player").GetAllByIndex("match_id", (*e).Match.ID).
		Update(map[string]interface{}{
			"status": psPlaying,
		}).RunWrite(session)

	fmt.Println("Joined.")

	if status == stReady {
		fmt.Println("Starting the server...")
		go serverStart((*e).Match.IP, (*e).Match.Port)
	}
}

func find(r *R, e *Event) {
	// check for existing game
	fmt.Printf("Checking for possible existing game...\n")

	var ps []Player
	cursor, err := db.Table("player").GetAllByIndex("account_id", (*e).User.ID).
		Run(session)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	defer cursor.Close()
	err = cursor.All(&ps)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	for _, p := range ps {
		if p.Status > int(psPlaying) { // left or done
			continue
		}

		var matches = make([]Match, 0)
		cursor2, err := db.Table("match").Get(p.Match).Run(session)
		if Error(err) {
			(*r).Error = errNoMatchFound
			break
		}
		defer cursor2.Close()
		err = cursor2.All(&matches)
		if Error(err) {
			(*r).Error = errNoMatchFound
			return
		}

		for _, m := range matches {
			if ((*e).Match.Typ != 0 && m.Typ != (*e).Match.Typ) ||
				m.Status > int(stPlay) {
				continue
			}
			fmt.Printf("Existing game found.\n")
			(*e).Match = &m
			join(r, e)
			return
		}
	}
	if (*e).Match.Typ == 0 {
		return
	}

	// no existing game, find a match
	fmt.Printf("Finding a match#%d for %s...\n", (*e).Match.Typ,
		(*e).User.Email)
	var matches = make([]Match, 0)
	found := false
	cursor3, err := db.Table("match").Filter(
		db.Row.Field("status").Eq(int(stReserved))).Filter(
		db.Row.Field("typ").Eq((*e).Match.Typ)).Run(session)
	// TODO order by counter desc
	if Error(err) {
		(*r).Error = errNoMatchFound
		return
	}
	defer cursor3.Close()
	err = cursor3.All(&matches)
	if Error(err) {
		(*r).Error = errNoMatchFound
		return
	}
	for _, m := range matches {
		found = true
		(*e).Match = &m
		break
	}
	if !found {
		create(r, e)
	}

	join(r, e)

	// get players
	players(r, e)
}

func players(r *R, e *Event) {
	fmt.Printf("Fetching players for match %s...\n", (*e).Match.ID)

	// match info
	cursor, err := db.Table("match").Get((*e).Match.ID).Run(session)
	if Error(err) {
		(*r).Error = errNoMatchFound
		return
	}
	defer cursor.Close()
	err = cursor.One(&(*e).Match)
	if Error(err) {
		(*r).Error = errNoMatchFound
		return
	}

	// player info
	(*e).Players = make([]Player, 0)
	var players = make([]Player, 0)
	cursor2, err := db.Table("player").GetAllByIndex("match_id", (*e).Match.ID).
		Run(session)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	defer cursor2.Close()
	err = cursor2.All(&players)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	for _, p := range players {
		if p.Status >= int(psLeft) {
			continue
		}
		(*e).Players = append((*e).Players, p)
	}
}

func leave(r *R, e *Event) {
	if (*e).User == nil {
		return
	}
	fmt.Printf("%s is leaving a game...\n", (*e).User.ID)

	cursor, err := db.Table("player").GetAllByIndex("account_id", (*e).User.ID).
		Run(session)
	if Error(err) {
		(*r).Error = errPlayerLeave
		return
	}
	defer cursor.Close()
	err = cursor.All(&(*e).Players)
	if Error(err) {
		(*r).Error = errPlayerLeave
		return
	}
	for _, p := range (*e).Players {
		if p.Status < int(psLeft) {
			// update player status
			_, err := db.Table("player").Get(p.ID).
				Update(map[string]interface{}{
					"status": int(psLeft),
				}).RunWrite(session)
			if Error(err) {
				(*r).Error = errPlayerLeave
				return
			}

			// reduce match's player counter
			cursor2, err := db.Table("match").Get(p.Match).Run(session)
			if Error(err) {
				return
			}
			defer cursor2.Close()
			err = cursor2.One(&(*e).Match)
			if Error(err) {
				return
			}
			if (*e).Match.Status != int(stReserved) {
				// do not make any process for any games rather than "reserved"
				continue
			}
			(*e).Match.Counter = (*e).Match.Counter - 1
			_, err = db.Table("match").Get((*e).Match.ID).
				Update(map[string]interface{}{
					"counter": (*e).Match.Counter,
				}).RunWrite(session)
			if Error(err) {
				return
			}
		}
	}
}

func character(r *R, e *Event) {
	if (*e).User == nil {
		fmt.Printf("User ID is not set! Cannot change the character\n")
		return
	}
	fmt.Printf("%s is choosing a character: %d\n", (*e).User.ID,
		(*e).Me.Character)

	// TODO use a better query after train on rethinkdb
	cursor, err := db.Table("player").GetAllByIndex("match_id", (*e).Match.ID).
		Run(session)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	defer cursor.Close()
	err = cursor.All(&(*e).Players)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	updated := false
	for _, p := range (*e).Players {
		if p.Player == (*e).User.ID && p.Status == int(psJoined) {
			result, err := db.Table("player").Get(p.ID).
				Update(map[string]interface{}{
					"character": (*e).Me.Character,
				}).RunWrite(session)
			if Error(err) || result.Errors != 0 {
				(*r).Error = errCharacterSelect
				return
			}
			updated = true
			break
		}
	}
	if !updated {
		(*r).Error = errCharacterSelect
		return
	}
}
