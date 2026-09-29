
using N2N_USER_SERVER.Bootstrap;

namespace N2N_USER_SERVER
{
    public class Program {
        static void Main() {
            Initialization.Init();//总初始化
            Bootstrap.HttpBootstrap.Setup();//Http初始化
        }
    }
}
