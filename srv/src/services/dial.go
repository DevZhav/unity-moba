package main

import (
	"bufio"
	"bytes"
	"fmt"
	"net"
	"strconv"
)

func Dial(host string, port int, message string) []byte {
	fmt.Printf("<dialing %s:%d with %s>\n", host, port, message)

	p := make([]byte, CommSize)
	conn, err := net.Dial("udp", host+":"+strconv.Itoa(port))
	Error(err)
	defer conn.Close()

	fmt.Fprintf(conn, message)
	n, err := bufio.NewReader(conn).Read(p)
	if err != nil {
		return []byte{}
	}

	p = bytes.Trim(p[0:n], "\x00")
	fmt.Printf("<received %s>\n", string(p))
	return p
}
