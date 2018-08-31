package main

import (
	"encoding/json"
	"fmt"

	db "gopkg.in/gorethink/gorethink.v4"
)

// Chars keeps all characters of the game
var Chars = make([]Character, 0)

// characters service
func characters(r *R) {
	DBCheck()

	act := Action((*r).Action)
	if act == ActPing {
		// ping
	} else if act == ActCharacters {
		charList(r)
	}

	(*r).Request = "" // clear the request
	if (*r).Error != 0 {
		(*r).Response = "{}"
	} else {
		j, err := json.Marshal(Chars)
		if Error(err) {
			(*r).Error = ErrSys
			return
		}
		(*r).Response = string(j)
	}
}

func charList(r *R) {
	fmt.Printf("Fetching all characters stats...\n")

	if len(Chars) > 0 {
		fmt.Printf("Using cache...\n")
		return
	}

	cursor, err := db.Table("character").Run(session)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	defer cursor.Close()
	err = cursor.All(&Chars)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
}
