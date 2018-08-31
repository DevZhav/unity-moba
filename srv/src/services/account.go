package main

import (
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"math/rand"
	"regexp"
	"time"

	"github.com/badoux/checkmail"
	db "gopkg.in/gorethink/gorethink.v4"
)

type userStatus int

const (
	usNone userStatus = iota
	usNotActive
	usActive
	usBanned
)

const (
	errLoginFailed Err = 101
	errKeyFailed   Err = 102
	errEmailExists Err = 103
	errRegInsert   Err = 104
	errResetAcc    Err = 105
	errRegCodeVal  Err = 106
	errRegCodeExp  Err = 107
	errRegCodeUpd  Err = 108
	errResCodeVal  Err = 109
	errResCodeExp  Err = 110
	errResCodeUpd  Err = 111
	errEmailInval  Err = 112
	errPassInval   Err = 113
	errNickInval   Err = 114
)

func account(r *R) {
	DBCheck()

	var acc Account
	if err := json.Unmarshal([]byte((*r).Request), &acc); err != nil {
		panic(err)
	}

	act := Action((*r).Action)
	if act == ActPing {
		// ping
	} else if act == ActLogin {
		login(r, &acc)
	} else if act == ActKey {
		key(r, &acc)
	} else if act == ActRegister {
		register(r, &acc)
	} else if act == ActRegisterCode {
		registerCode(r, &acc)
	} else if act == ActReset {
		reset(r, &acc)
	} else if act == ActResetCode {
		resetCode(r, &acc)
	}

	(*r).Request = "" // clear the request
	acc.Password = "" // clear the password if any

	j, err := json.Marshal(acc)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	(*r).Response = string(j)
}

func keyup(uid string, ip string) string {
	// creates a new key for the logged in user
	fmt.Printf("Creating a new key for the user %s...\n", uid)
	rand := time.Now().Unix()
	key := fmt.Sprintf("%x", sha256.Sum256([]byte(string(rand))))[:45]

	result, err := db.Table("account").Get(uid).Update(map[string]interface{}{
		"key": key,
		"ip":  ip,
	}).RunWrite(session)
	if Error(err) || result.Errors != 0 {
		fmt.Printf("Key creating is failed. Rejecting login.\n")
		return ""
	}
	return key
}

// passwords are already hashed with MD5
func checkpass(pass string) bool {
	// TODO
	//r1 := regexp.MustCompile("[0-9]+")
	//r2 := regexp.MustCompile("[A-Z]+")
	//r3 := regexp.MustCompile(".{8,}")
	//return r1.MatchString(pass) && r2.MatchString(pass) && r3.MatchString(pass)

	if len(pass) == 32 {
		return true
	}
	return false
}

func checknick(nick string) bool {
	re := regexp.MustCompile("^[a-zA-Z0-9]*$")
	v := re.MatchString(nick)
	if v {
		if len(nick) > 13 {
			v = false
		}
	}
	return v
}

func login(r *R, acc *Account) {
	fmt.Printf("Logining in from %s...\n", (*r).Addr.String())

	// validate email format
	err := checkmail.ValidateFormat((*acc).Email)
	if len((*acc).Email) < 8 || err != nil {
		fmt.Printf("Invalid email address: %s\n", (*acc).Email)
		(*r).Error = errLoginFailed
		return
	}

	// validate pass
	if !checkpass((*acc).Password) {
		fmt.Printf("Invalid password entry: %s\n", (*acc).Password)
		(*r).Error = errLoginFailed
		return
	}

	sha := sha256.Sum256([]byte((*acc).Password))
	hash := hex.EncodeToString(sha[:])

	var accTemp *Account
	cursor, err := db.Table("account").GetAllByIndex("email", (*acc).Email).
		Run(session)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	defer cursor.Close()
	err = cursor.One(&accTemp)
	if Error(err) || accTemp == nil || accTemp.Password != hash {
		fmt.Printf("Login failed for %s\n", (*acc).Email)
		(*r).Error = errLoginFailed
		return
	}
	fmt.Printf("Login success for %s\n", accTemp.Email)

	accTemp.Key = keyup(accTemp.ID, (*r).Addr.IP.String())
	if accTemp.Key == "" {
		(*r).Error = errLoginFailed
		return
	}

	(*acc) = *accTemp
}

func key(r *R, acc *Account) {
	fmt.Printf("Authorizing %s from %s... ", (*acc).Email, (*r).Addr.String())

	var accTemp *Account
	cursor, err := db.Table("account").GetAllByIndex("email", (*acc).Email).
		Run(session)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	defer cursor.Close()
	err = cursor.One(&accTemp)
	if Error(err) || accTemp == nil || accTemp.IP != (*r).Addr.IP.String() ||
		accTemp.Key != (*acc).Key {
		fmt.Printf("Key login failed!\n")
		(*r).Error = errKeyFailed
		return
	}

	j, err := json.Marshal(*acc)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	(*r).Response = string(j)
}

func register(r *R, acc *Account) {
	fmt.Printf("Registration from %s for %s...\n", (*r).Addr.String(),
		(*acc).Email)

	// validate email and email account
	if len((*acc).Email) > 0 {
		err := checkmail.ValidateHost((*acc).Email)
		if smtpErr, ok := err.(checkmail.SmtpError); ok && err != nil {
			fmt.Printf("Invalid email address: %s %s", smtpErr.Code(), smtpErr)
			(*r).Error = errEmailInval
			return
		}
	}

	// validate pass
	if !checkpass((*acc).Password) {
		fmt.Printf("Invalid password entry: %s\n", (*acc).Password)
		(*r).Error = errPassInval
		return
	}

	// validate nick
	if !checknick((*acc).Nickname) {
		fmt.Printf("Invalid nickname: %s\n", (*acc).Nickname)
		(*r).Error = errNickInval
		return
	}

	var accTemp *Account

	// email existence
	cursor, err := db.Table("account").GetAllByIndex("email", (*acc).Email).
		Run(session)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	defer cursor.Close()
	cursor.One(&accTemp)

	if accTemp != nil {
		fmt.Printf("This e-mail is already registered!\n")
		(*r).Error = errEmailExists
		return
	}

	sha := sha256.Sum256([]byte((*acc).Password))
	hash := hex.EncodeToString(sha[:])

	t := time.Now().Unix()
	key := fmt.Sprintf("%x", sha256.Sum256([]byte(string(t))))[:45]

	rand.Seed(t)
	code := rand.Intn(999999-100000) + 100000

	result, err := db.Table("account").Insert(map[string]interface{}{
		"email":      (*acc).Email,
		"password":   hash,
		"nickname":   (*acc).Nickname,
		"creation":   int32(time.Now().Unix()),
		"key":        key,
		"ip":         (*r).Addr.IP.String(),
		"reset":      code,
		"resetvalid": int32(time.Now().Unix()) + 86400, // 48 hours,
		"status":     0,
	}).RunWrite(session)
	if Error(err) || result.Errors != 0 {
		fmt.Printf("An error occured while registering a user.\n")
		(*r).Error = errRegInsert
		return
	}
	(*acc).ID = result.GeneratedKeys[0]

	fmt.Printf("Registration code: %d\n", code)
	// TODO send email (activation code)
}

func registerCode(r *R, acc *Account) {
	fmt.Printf("Activation from %s for %s...\n", (*r).Addr.String(),
		(*acc).Email)

	// validate email format
	err := checkmail.ValidateFormat((*acc).Email)
	if len((*acc).Email) < 8 || err != nil {
		fmt.Printf("Invalid email address: %s\n", (*acc).Email)
		(*r).Error = errRegCodeVal
		return
	}

	var accTemp *Account
	// email and code existence
	cursor, err := db.Table("account").GetAllByIndex("email", (*acc).Email).
		Run(session)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	defer cursor.Close()
	cursor.One(&accTemp)

	if accTemp == nil || (*acc).Reset != accTemp.Reset {
		fmt.Printf("Invalid combination of e-mail and code.\n")
		(*r).Error = errRegCodeVal
		return
	}

	t := time.Now().Unix()

	fmt.Printf("User: %s\n", (*acc).ID)
	fmt.Printf("Time: %d\n", t)
	fmt.Printf("Expires: %d\n", accTemp.ResetValid)

	if int(t) > accTemp.ResetValid {
		fmt.Printf("The activation code is expired.\n")
		(*r).Error = errRegCodeExp
		return
	}

	rand.Seed(t)
	code := rand.Intn(999999-100000) + 100000

	result, err := db.Table("account").Get(accTemp.ID).
		Update(map[string]interface{}{
			"reset":      code,
			"resetvalid": 0,
			"status":     usActive,
		}).RunWrite(session)

	if Error(err) || result.Errors != 0 {
		fmt.Printf("An error occured while activating the user\n")
		(*r).Error = errRegCodeUpd
		return
	}
}

func reset(r *R, acc *Account) {
	fmt.Printf("Requesting for a password reset from %s for %s...\n",
		(*r).Addr.String(), (*acc).Email)

	// validate email format
	err := checkmail.ValidateFormat((*acc).Email)
	if len((*acc).Email) < 8 || err != nil {
		fmt.Printf("Invalid email address: %s\n", (*acc).Email)
		(*r).Error = errEmailInval
		return
	}

	timer := int(time.Now().Unix())
	// find account with e-mail only
	cursor, err := db.Table("account").GetAllByIndex("email", (*acc).Email).
		Run(session)
	if Error(err) {
		(*r).Error = ErrSys
	}
	defer cursor.Close()
	err = cursor.One(acc)
	if Error(err) || timer > (*acc).ResetValid {
		fmt.Printf("Account is not found or the reset code is expired.\n")
		(*r).Error = ErrHidden
		return
	}

	rand.Seed(time.Now().Unix())
	reset := rand.Intn(99999-10000) + 10000
	resetValid := int32(time.Now().Unix()) + (60 * 5) // valid for 5 minutes

	result, err := db.Table("account").Get((*acc).ID).
		Update(map[string]interface{}{
			"reset":      reset,
			"resetvalid": resetValid,
		}).RunWrite(session)
	if Error(err) || result.Errors != 0 {
		fmt.Printf("A problem occured while updating the reset code.\n")
		(*r).Error = ErrHidden
		return
	}

	fmt.Printf("Reset code: %d\n", reset)
	// TODO send email
}

func resetCode(r *R, acc *Account) {
	fmt.Printf("Rseetting password from %s for %s...\n",
		r.Addr.String(), (*acc).Email)

	// validate email format
	err := checkmail.ValidateFormat((*acc).Email)
	if len((*acc).Email) < 8 || err != nil {
		fmt.Printf("Invalid email address: %s\n", (*acc).Email)
		(*r).Error = errResCodeVal
		return
	}

	// validate pass
	if !checkpass((*acc).Password) {
		fmt.Printf("Invalid password entry: %s\n", (*acc).Password)
		(*r).Error = errResCodeVal
		return
	}

	// email and code existence
	var accTemp *Account
	cursor, err := db.Table("account").GetAllByIndex("email", (*acc).Email).
		Run(session)
	if Error(err) {
		(*r).Error = ErrSys
		return
	}
	defer cursor.Close()
	err = cursor.One(&accTemp)
	if Error(err) || accTemp == nil || (*acc).Reset != accTemp.Reset {
		fmt.Printf("Invalid combination of e-mail and code.\n")
		(*r).Error = errResCodeVal
		return
	}

	t := time.Now().Unix()
	if int(t) > accTemp.ResetValid {
		fmt.Printf("The reset code is expired.\n")
		(*r).Error = errResCodeExp
		return
	}

	rand.Seed(t)
	code := rand.Intn(999999-100000) + 100000

	sha := sha256.Sum256([]byte(accTemp.Password))
	hash := hex.EncodeToString(sha[:])

	result, err := db.Table("account").Get((*acc).ID).
		Update(map[string]interface{}{
			"password":   hash,
			"reset":      code,
			"resetvalid": 0,
			"status":     usActive,
		}).RunWrite(session)
	if Error(err) || result.Errors != 0 {
		fmt.Printf("An error occured while resetting the password.\n")
		(*r).Error = errResCodeUpd
		return
	}
}
