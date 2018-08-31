using System.Net.Mail;
using System.Text.RegularExpressions;

public static class Validation {

    public static bool IsValidEmail(string email)
    {
        if (email.Length < 8) return false;
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsValidPassword(string password)
    {
        Regex hasNumber = new Regex(@"[0-9]+");
        Regex hasUpperChar = new Regex(@"[A-Z]+");
        Regex hasMinimum8Chars = new Regex(@".{8,}");

        return hasNumber.IsMatch(password) && hasUpperChar.IsMatch(password) && 
            hasMinimum8Chars.IsMatch(password);
    }

    public static bool IsValidNick(string nick)
    {
        Regex r = new Regex("^[a-zA-Z0-9]*$");
        bool valid = r.IsMatch(nick);
        if(valid)
        {
            if (nick.Length > 13) valid = false;
        }
        return valid;
    }

    public static bool IsValidCode(string code)
    {
        Regex onlyNumbers = new Regex("^[0-9]*$");
        Regex only6Numbers = new Regex(@".{6,}");

        return onlyNumbers.IsMatch(code) && only6Numbers.IsMatch(code);
    }
}
