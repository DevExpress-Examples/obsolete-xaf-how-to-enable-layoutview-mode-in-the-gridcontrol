using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.BaseImpl;

namespace WinSolution.Module {
    public class Updater : ModuleUpdater {
        public Updater(IObjectSpace objectSpace, Version currentDBVersion) : base(objectSpace, currentDBVersion) { }
        public override void UpdateDatabaseAfterUpdateSchema() {
            base.UpdateDatabaseAfterUpdateSchema();
            Person person1 = ObjectSpace.CreateObject<Person>();
            person1.FirstName = "Sam";
            person1.Email = "sam@example.com";
            Person person2 = ObjectSpace.CreateObject<Person>();
            person2.FirstName = "John";
            person2.Email = "john@example.com";
            Task task1 = ObjectSpace.CreateObject<Task>();
            task1.Subject = "Task1";
            task1.DueDate = DateTime.Today;
            Task task2 = ObjectSpace.CreateObject<Task>();
            task2.Subject = "Task2";
            task1.DueDate = DateTime.Today;
        }
    }
}
