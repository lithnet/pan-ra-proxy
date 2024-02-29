using System.ServiceProcess;

namespace Lithnet.Pan.RAProxy
{
    public partial class RAProxyService : ServiceBase
    {
        public RAProxyService()
        {
            this.InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Program.Start();
        }

        protected override void OnStop()
        {
            Program.Stop();
        }
    }
}
