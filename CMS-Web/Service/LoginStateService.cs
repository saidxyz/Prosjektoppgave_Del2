namespace CMS_Web.Service
{
    public class LoginStateService
    {
        public bool IsLoggedIn { get; set; }

        public void SetLoginStatus(bool status)
        {
            IsLoggedIn = status;
        }
    }

}