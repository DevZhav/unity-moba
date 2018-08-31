package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"net"
)

func serverUDP(service string) {
	host := ""
	port := 0
	for _, s := range Config.Services {
		if service == s.Name {
			host = s.Host
			port = s.Port
			break
		}
	}
	fmt.Printf("!!! Activate usage of host later: %s\n", host)
	srv := net.UDPAddr{
		Port: port,
	}

	fmt.Printf("Starting listener for %s @ [%s]:[%d]\n",
		service, srv.IP, srv.Port)
	conn, err := net.ListenUDP("udp", &srv)
	Error(err)
	defer conn.Close()

	buf := make([]byte, CommSize)
	var msg []byte

	for {
		n, addr, err := conn.ReadFromUDP(buf)
		msg = bytes.Trim(buf[0:n], "\x00")
		fmt.Println("< ", string(msg), " from ", addr)

		if err != nil {
			fmt.Println("# [read error] ", err)
		} else {
			var r R
			if err := json.Unmarshal(msg, &r); err != nil {
				panic(err)
			}
			r.Addr = addr
			r.Error = ErrNone

			if service == "account" {
				account(&r)
			} else if service == "match" {
				match(&r)
			} else if service == "characters" {
				characters(&r)
			} else if service == "priv" {
				priv(&r)
			}

			j, err := json.Marshal(r)
			if err != nil {
				fmt.Println(err)
			} else {
				fmt.Printf("> %s >> %s ", string(j), addr)
				_, err := conn.WriteToUDP(j, addr)
				if err != nil {
					fmt.Printf("%+v", err)
				}
			}
		}
		fmt.Printf("\n\n")
	}
}
