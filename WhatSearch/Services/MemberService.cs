namespace WhatSearch.Core
{
    public class MemberService : IMemberService
    {
        //static SystemConfig config = Ioc.GetConfig();
        public bool Validate(string loginId, string password, out string message)
        {
            //Type type = Type.GetType(typeName);
            //ILoginProvider loginProvider = Activator.CreateInstance(type) as ILoginProvider;            
            ILoginProvider loginProvider = Ioc.Get<ILoginProvider>();
            return loginProvider.Validate(loginId, password, out message);
        }        
    }
}
