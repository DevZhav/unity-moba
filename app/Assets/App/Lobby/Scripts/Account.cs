using System;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// User status types.
/// </summary>
public enum UserStatus
{
    None,
    NotActivated,
    Logined,
    Banned,
    AnotherPlayer,
};

/// <summary>
/// User structure
/// </summary>
[Serializable]
public class User
{
    /// <summary>
    /// GUID of the user
    /// </summary>
    public string ID { get; set; }

    /// <summary>
    /// E-mail of the user
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// MD5 hashed password of the user
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// Hashed key from the server
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Nickname of the user
    /// </summary>
    public string Nickname { get; set; }

    /// <summary>
    /// Activation code for registration and password reset
    /// </summary>
    public int Reset { get; set; }

    /// <summary>
    /// E-mail and key header to send on every request that requires login
    /// </summary>
    private string header = "";

    /// <summary>
    /// Status of the user
    /// </summary>
    private UserStatus status = 0;

    /// <summary>
    /// get-set function for user status
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public UserStatus Status(UserStatus s=UserStatus.None)
    {
        if (s == UserStatus.None) return status;
        status = s;
        return status;
    }

    public string Header(bool h=false)
    {
        if (!h) return header;
        header = Email + " " + Key;
        return header;
    }

    public UserStatus GetStatus()
    {
        return status;
    }

    public void ResetStatus()
    {
        status = UserStatus.None;
    }
}


/// <summary>
/// Account class
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Kill2Live))]
public class Account : MonoBehaviour {

    /// <summary>
    /// Self script for static operations
    /// </summary>
    public static Account self = null;

    /// <summary>
    /// Single/Owner user structure
    /// </summary>
    private User user;

    /// <summary>
    /// UI panel types.
    /// </summary>
    public enum Panel
    {
        Login,
        Register,
        RegisterCode,
        Reset,
        ResetCode,
        Profile,
    };

    /// UI panels for all account services
    public GameObject uiLogin;
    public GameObject uiRegister;
    public GameObject uiRegisterCode;
    public GameObject uiReset;
    public GameObject uiResetCode;
    public GameObject uiProfile;

    // login ui elements
    public InputField uiLoginEmail;
    public InputField uiLoginPassword;

    // register ui elements
    public InputField uiRegisterEmail;
    public InputField uiRegisterPassword;
    public InputField uiRegisterPasswordConfirm;
    public InputField uiRegisterNick;

    // register code ui elements
    public InputField uiRegisterCodeCode;

    // reset password ui elements
    public InputField uiResetEmail;

    // reset password code ui elements
    public InputField uiResetCodeCode;
    public InputField uiResetCodePassword;
    public InputField uiResetCodePasswordConfirm;

    // profile elements
    public Text nick;


    /// <summary>
    /// fake constructor
    /// </summary>
    private void Awake()
    {
        self = this;
    }

    /// <summary>
    /// Checks if user is logged in or not
    /// </summary>
    /// <returns></returns>
    public static bool IsLoggedIn()
    {
        if (self.user != null && self.user.GetStatus() == UserStatus.Logined)
            return true;
        return false;
    }

    public static User GetUser()
    {
        if (self == null) return null;
        return self.user;
    }

    public static void Clear()
    {
        if (self == null) return;
        self.user.ResetStatus();
        self.nick.text = "";
        self.user = null;
    }

    
    /// <summary>
    /// Logs in a user
    /// </summary>
    /// <param name="r">UDPPacket</param>
    public bool Login(UDPPacket r=null)
    {
        if (r == null)
        {
            Debug.Log("Signin in...");

            User user = new User
            {
                Email = uiLoginEmail.text,
                Password = Encryption.Hash(uiLoginPassword.text),
            };

            UDP.Request(new UDPPacket
            {
                Service = UDPService.Account,
                Action = UDPAction.Login,
                Request = JsonConvert.SerializeObject(user),
            });
            uiLoginPassword.text = "";
            return false;
        } else
        {
            if (r.Error != 0)
            {
                if (r.Error >= (int)ServiceError.AccountUnknown)
                {
                    UIMessageGlobal.Open(Language.v["loginfailed"],
                        Language.v["loginfaileddesc"]);
                    Debug.LogWarning(r.ToString());
                }
                return false;
            }

            user = JsonConvert.DeserializeObject<User>(r.Response);
            user.Status(UserStatus.Logined);

            nick.text = user.Nickname;
           
            Debug.Log("Signed: " + user.Email);
            return true;
        }
    }
    public void LoginUI() {
        if (!Validation.IsValidEmail(uiLoginEmail.text))
            UIMessageGlobal.Open(Language.v["validEmail"]);
        else if (!Validation.IsValidPassword(uiLoginPassword.text))
            UIMessageGlobal.Open(Language.v["validPass"],
                Language.v["validPassDetail"]);
        else
            Login();
    }
    
    /// <summary>
    /// Registers a user with e-mail
    /// </summary>
    /// <param name="r"></param>
    public bool Register(UDPPacket r=null)
    {
        if (r == null)
        {
            Debug.Log("Creating a new account...");

            User user = new User
            {
                Email = uiRegisterEmail.text,
                Password = Encryption.Hash(uiRegisterPassword.text),
                Nickname = uiRegisterNick.text,
            };

            UDP.Request(new UDPPacket
            {
                Service = UDPService.Account,
                Action = UDPAction.Register,
                Request = JsonConvert.SerializeObject(user),
            });
            uiRegisterPassword.text = "";
            uiRegisterPasswordConfirm.text = "";
            return false;
        }
        else
        {
            if (r.Error != 0)
            {
                if (r.Error >= (int)ServiceError.AccountUnknown)
                {
                    UIMessageGlobal.Open(Language.v["registerfailed"],
                    Language.v["registerfaileddesc"]);
                    Debug.LogWarning(r.ToString());
                }
                return false;
            }

            Debug.Log("Registration is successful");
            return true;
        }
    }
    public void RegisterUI() {
        if (!Validation.IsValidEmail(uiRegisterEmail.text))
            UIMessageGlobal.Open(Language.v["validEmail"]);
        else if (!Validation.IsValidNick(uiRegisterNick.text))
            UIMessageGlobal.Open(Language.v["validNick"],
                Language.v["validNickDetail"]);
        else if (!Validation.IsValidPassword(uiRegisterPassword.text))
            UIMessageGlobal.Open(Language.v["validPass"],
                Language.v["validPassDetail"]);
        else if(uiRegisterPassword.text != uiRegisterPasswordConfirm.text)
            UIMessageGlobal.Open(Language.v["validPassMatch"]);
        else
            Register();
    }

    /// <summary>
    /// Activates a registered user with the code which is sent by e-mail
    /// </summary>
    /// <param name="r"></param>
    /// <returns></returns>
    public bool RegisterCode(UDPPacket r=null)
    {
        if (r == null)
        {
            Debug.Log("Activating an account...");

            int code = 0;
            if (int.TryParse(uiRegisterCodeCode.text, out code))
            {
                User user = new User
                {
                    Email = uiRegisterEmail.text,
                    Reset = code,
                };

                UDP.Request(new UDPPacket
                {
                    Service = UDPService.Account,
                    Action = UDPAction.RegisterCode,
                    Request = JsonConvert.SerializeObject(user),
                });
            }
            return false;
        }
        else
        {
            if (r.Error != 0)
            {
                if (r.Error >= (int)ServiceError.AccountUnknown)
                {
                    UIMessageGlobal.Open(Language.v["registercodefailed"],
                    Language.v["registercodefaileddesc"]);
                    Debug.LogWarning(r.ToString());
                }
                return false;
            }

            Debug.Log("Registered user is activated.");
            // TODO success message
            return true;
        }
    }
    public void RegisterCodeUI() {
        if (!Validation.IsValidCode(uiRegisterCodeCode.text))
            UIMessageGlobal.Open(Language.v["validCode"]);
        else
            RegisterCode();
    }

    /// <summary>
    /// Sends a password reset code to user's e-mail (if exists)
    /// </summary>
    /// <param name="r"></param>
    public bool Reset(UDPPacket r=null)
    {
        if (r == null)
        {
            Debug.Log("Resetting password...");

            User user = new User
            {
                Email = uiResetEmail.text,
            };

            UDP.Request(new UDPPacket
            {
                Service = UDPService.Account,
                Action = UDPAction.Reset,
                Request = JsonConvert.SerializeObject(user),
            });
            return false;
        }
        else
        {
            if (r.Error != 0)
            {
                if (r.Error >= (int)ServiceError.AccountUnknown)
                {
                    UIMessageGlobal.Open(Language.v["resetfailed"],
                    Language.v["resetfaileddesc"]);
                    Debug.LogWarning(r.ToString());
                }
                return false;
            }

            Debug.Log("Password reset link is sent");
            return true;
        }
    }
    public void ResetUI() {
        if (!Validation.IsValidEmail(uiResetEmail.text))
            UIMessageGlobal.Open(Language.v["validEmail"]);
        else
            Reset();
    }

    /// <summary>
    /// Resets a password with the code which is sent by e-mail
    /// </summary>
    /// <param name="r"></param>
    public bool ResetCode(UDPPacket r=null)
    {
        if(r == null)
        {
            Debug.Log("Resetting a password with code...");

            int code = 0;
            if (int.TryParse(uiResetCodeCode.text, out code))
            {
                User user = new User
                {
                    Email = uiResetEmail.text,
                    Reset = code,
                    Password = Encryption.Hash(uiResetCodePassword.text),
                };

                UDP.Request(new UDPPacket
                {
                    Service = UDPService.Account,
                    Action = UDPAction.ResetCode,
                    Request = JsonConvert.SerializeObject(user),
                });
            }
            return false;
        }
        else
        {
            if (r.Error != 0)
            {
                if (r.Error >= (int)ServiceError.AccountUnknown)
                {
                    UIMessageGlobal.Open(Language.v["resetcodefailed"],
                    Language.v["resetcodefaileddesc"]);
                    Debug.LogWarning(r.ToString());
                }
                return false;
            }

            Debug.Log("Password reset is completed.");
            return true;
        }
    }
    public void ResetCodeUI() {
        if (!Validation.IsValidCode(uiResetCodeCode.text))
            UIMessageGlobal.Open(Language.v["validCode"]);
        else if (!Validation.IsValidPassword(uiResetCodePassword.text))
            UIMessageGlobal.Open(Language.v["validPass"],
                Language.v["validPassDetail"]);
        else if (uiResetCodePassword.text != uiResetCodePasswordConfirm.text)
            UIMessageGlobal.Open(Language.v["validPassMatch"]);
        else
            ResetCode();
    }

    public void PanelShow(string panel)
    {
        // first, deactivate all panels
        uiLogin.SetActive(false);
        uiRegister.SetActive(false);
        uiRegisterCode.SetActive(false);
        uiReset.SetActive(false);
        uiResetCode.SetActive(false);

        try
        {
            Panel panelType = 
                (Panel)Enum.Parse(typeof(Panel), panel);
            switch(panelType)
            {
                case Panel.Login: uiLogin.SetActive(true); break;
                case Panel.Register: uiRegister.SetActive(true); break;
                case Panel.RegisterCode: uiRegisterCode.SetActive(true); break;
                case Panel.Reset: uiReset.SetActive(true); break;
                case Panel.ResetCode: uiResetCode.SetActive(true); break;
            }
        }
        catch (Exception)
        {
            Debug.LogErrorFormat("Can't convert {0} to enum AccountUIPanel",
                panel);
        }
    }
}
