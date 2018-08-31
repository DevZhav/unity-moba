package main

import (
	"encoding/json"

	db "gopkg.in/gorethink/gorethink.v4"
)

// priv service
func priv(r *R) {
	DBCheck()

	// TODO add key protection because this is not meant to be used by users

	act := Action((*r).Action)
	if act == ActPing {
		// ping
	} else if act == ActMatches {
		privMatches(r)
	} else if act == ActMatch {
		privMatch(r)
	} else if act == ActAccount {
		privAccount(r)
	} else if act == ActCID {
		privMatch(r)
	}

	(*r).Request = "" // clear the request
}

func privMatches(r *R) {
	var matches = make([]Match, 0)

	// TODO check
	cursor, err := db.Table("match").Run(session)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	defer cursor.Close()
	err = cursor.All(&matches)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}

	j, err := json.Marshal(matches)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}

	(*r).Response = string(j)
}

func privMatch(r *R) {
	var event Event

	if err := json.Unmarshal([]byte((*r).Request), &event); err != nil {
		(*r).Error = ErrSys
		return
	}

	cursor, err := db.Table("match").Get(event.Match.ID).Run(session)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	defer cursor.Close()
	err = cursor.One(&event.Match)
	if Error(err) {
		// (*r).Error = ErrSys
		// no match found, do not give any error
		return
	}

	cursor2, err := db.Table("player").GetAllByIndex("match_id",
		event.Match.ID).Run(session)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	defer cursor2.Close()
	err = cursor2.All(&event.Players)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}

	j, err := json.Marshal(event)
	if err != nil {
		(*r).Error = ErrSys
		return
	}

	(*r).Response = string(j)
}

func privAccount(r *R) {
	var event Event

	if err := json.Unmarshal([]byte((*r).Request), &event); err != nil {
		(*r).Error = ErrSys
		return
	}

	cursor, err := db.Table("account").Get(event.User.ID).Run(session)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	defer cursor.Close()
	err = cursor.One(&event.User)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	event.User.Password = ""
	event.User.Key = ""

	j, err := json.Marshal(event)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}

	(*r).Response = string(j)
}
