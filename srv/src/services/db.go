package main

import (
	"os"
	"time"

	db "gopkg.in/gorethink/gorethink.v4"
)

var dbname = "kill2live"
var session *db.Session
var err error

// Account is the account table
type Account struct {
	ID         string `gorethink:"id" json:"ID,omitable"`
	Email      string `gorethink:"email" json:"Email,omitable"`
	Password   string `gorethink:"password" json:"Password,omitable"`
	Nickname   string `gorethink:"nickname" json:"Nickname,omitable"`
	Creation   int    `gorethink:"creation"`
	Key        string `gorethink:"key" json:"Key,omitable"`
	IP         string `gorethink:"ip" json:"IP,omitable"`
	Reset      int    `gorethink:"reset" json:"Reset,omitable"`
	ResetValid int    `gorethink:"resetvalid"`
	Status     int    `gorethink:"status"`
}

// Character is the character table
type Character struct {
	ID      string `gorethink:"id" json:"ID,omitable"`
	Health  int    `gorethink:"health" json:"Health,omitable"`
	Stamina int    `gorethink:"stamina" json:"Stamina,omitable"`
	Speed   int    `gorethink:"speed" json:"Speed,omitable"`
	Sword   int    `gorethink:"sword" json:"Sword,omitable"`
	Bow     int    `gorethink:"bow" json:"Bow,omitable"`
	Shield  int    `gorethink:"shield" json:"Shield,omitable"`
	Tag     string `gorethink:"tag" json:"Tag,omitable"`
}

// Match is the match table
type Match struct {
	ID       string `gorethink:"id" json:"ID,omitable"`
	Creation int    `gorethink:"creation"`
	Typ      int    `gorethink:"typ" json:"Typ,omitable"`
	Status   int    `gorethink:"status" json:"Status,omitable"`
	Counter  int    `gorethink:"counter" json:"Counter,omitable"`
	IP       string `gorethink:"ip" json:"IP,omitable"`
	Port     int    `gorethink:"port" json:"Port,omitable"`
	Key      string `gorethink:"key" json:"Key,omitable"`
}

// Player is the player table
type Player struct {
	ID         string `gorethink:"id" json:"ID,omitable"`
	Match      string `gorethink:"match_id,reference" gorethink_ref:"id" json:"Match_ID,omitable"`
	Player     string `gorethink:"account_id,reference" gorethink_ref:"id" json:"Account_ID,omitable"`
	PlayerName string `gorethink:"playername" json:"PlayerName,omitable"`
	Status     int    `gorethink:"status" json:"Status,omitable"`
	Side       int    `gorethink:"side" json:"Side,omitable"`
	Character  int    `gorethink:"character" json:"Character,omitable"`
	IP         string `gorethink:"ip" json:"IP,omitable"`
	Port       int    `gorethink:"port" json:"Port,omitable"`
	CID        int    `gorethink:"connection_id" json:"CID,omitable"`
}

// Settings is the settings table
type Settings struct {
	ID    string `gorethink:"id" json:"ID,omitable"`
	Key   string `gorethink:"key" json:"Key,omitable"`
	Value string `gorethink:"value" json:"Value,omitable"`
}

// DB connects to rethinkdb
func DB() {
	session, err = db.Connect(db.ConnectOpts{
		Addresses:  Config.DB.Addresses,
		InitialCap: Config.DB.InitialCap,
		MaxOpen:    Config.DB.MaxOpen,
		Timeout:    time.Duration(Config.DB.Timeout) * time.Second,
		Database:   dbname,
	})
	Error(err)

	if Config.DB.Debug {
		db.SetVerbose(true)
		db.Log.Out = os.Stderr
	}
}

// DBConnection returns the status of the database connection
func DBConnection() bool {
	return session.IsConnected()
}

// DBReconnect reconnects to the database
func DBReconnect() {
	session.Reconnect()
}

//DBCheck checks the database connection, if it is not connected, tries to
func DBCheck() {
	if !DBConnection() {
		DBReconnect()
	}
}
