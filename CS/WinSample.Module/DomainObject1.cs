using System;

using DevExpress.Xpo;

using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;

namespace WinSample.Module {
    [DefaultClassOptions]
    public class DomainObject1 : Person {
        public DomainObject1(Session session) : base(session) { }
    }

}
