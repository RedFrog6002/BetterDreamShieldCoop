using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterDreamShieldCoop
{
    public interface ICoop
    {
        public void Up(bool held);
        public void Down(bool held);
        public void Left(bool held);
        public void Right(bool held);
        public void Teleport(bool held);
        public void Special1(bool held);
        public void Special2(bool held);
        public void Special3(bool held);
        public void Special4(bool held);
        public void DestroyCoop();
    }
}
