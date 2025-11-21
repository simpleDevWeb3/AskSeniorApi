namespace AskSeniorApi.Models;

public class Auth
{
    public class SignUpRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }

        public string name { get; set; }

        public string? avatar_url { get; set; }


        public string? banner_url { get; set; }

        public string? bio { get; set; }
    }

    public class LogInRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

 

}
