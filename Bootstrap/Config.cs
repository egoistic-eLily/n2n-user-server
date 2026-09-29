namespace N2N_USER_SERVER.Bootstrap
{
    public enum CertMode
    {
        Default,
        Manual
    }
    public enum UserDbMode
    {
        Default,
        Manual
    }

    public class Config
    {
        public CertMode certmode {  get; set; }
        public string pfxpath { get; set; }
        public string pfxpassword { get; set; }
        public string url { get; set; }
        public int port { get; set; }
        public UserDbMode userdbmode { get; set; }
        public string userdbpath { get; set; }
        public string admin_username { get; set; }
        public string admin_password { get; set; }
    }
}