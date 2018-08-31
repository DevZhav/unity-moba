package main

import (
	"encoding/json"
	"fmt"
	"net"
	"os"
)

// CommSize is size of any communication
var CommSize = 2048

// db structure
type dbConfig struct {
	Addresses  []string `json:"addresses"`
	Port       int      `json:"port"`
	InitialCap int      `json:"initialCap"`
	MaxOpen    int      `json:"maxOpen"`
	Timeout    int      `json:"timeout"`
	Debug      bool     `json:"debug"`
}

// service structure
type service struct {
	Name string `json:"name"`
	Host string `json:"host"`
	Port int    `json:"port"`
}

// server executable structure
type server struct {
	Path string `json:"path"`
}

// configuration for services
type configuration struct {
	DB       dbConfig  `json:"db"`
	Services []service `json:"services"`
	Server   server    `json:"server"`
}

// ServicePort return port number of the requested service
func (c configuration) ServicePort(s string) int {
	for _, i := range c.Services {
		if i.Name == s {
			return i.Port
		}
	}
	return 0
}

// Err is a code for pre-defined errors
type Err int

// Action is a type of pre-defined action
type Action int

// General error codes
const (
	ErrNone   Err = 0
	ErrHidden Err = 1
	ErrAuth   Err = 2
	ErrSys    Err = 3
)

// R is the main communication structure
type R struct {
	ID       string       `json:"ID"`       // communication id
	Action   int          `json:"Action"`   // action type
	IP       string       `json:"IP"`       // ip address of the request
	Request  string       `json:"Request"`  // request string as json
	Response string       `json:"Response"` // response string as json
	Error    Err          `json:"Error"`    // error code
	Addr     *net.UDPAddr // udp address
}

// Action types
const (
	ActPing         Action = iota // 0
	ActLogin                      // 1
	ActKey                        // 2
	ActRegister                   // 3
	ActRegisterCode               // 4
	ActReset                      // 5
	ActResetCode                  // 6
	ActJoin                       // 7
	ActPlayers                    // 8
	ActLeave                      // 9
	ActCharacter                  // 10
	ActCharacters                 // 11
	ActMatches                    // 12
	ActMatch                      // 13
	ActAccount                    // 14
	ActCID                        // 15
)

// Config contains the service configuration
var Config configuration

// Error is general error function
func Error(err error) bool {
	if err != nil {
		fmt.Println(err)
		fmt.Println()
		return true
	}
	return false
}

// main thread
func main() {
	// load configuration
	configFile, err := os.Open("config.json")
	if err != nil {
		panic(err)
	}
	configJSON := json.NewDecoder(configFile)
	configJSON.Decode(&Config)

	// choose service by command line argument
	service := "account"
	args := os.Args[1:]
	if len(args) > 0 {
		service = args[0]
	}

	// database connection
	DB()

	// UDP listener
	serverUDP(service)
}
