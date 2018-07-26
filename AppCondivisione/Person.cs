using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace AppCondivisione
{
    public class Person
    {

        private bool isOld;
        private string name;
        private string surname;
        private string state;
        private IPAddress ip;
        private int port;
        private bool imNew;
        private System.Timers.Timer t;
        private string _Username;
        public string Username
        {
            get { return this._Username; }
            set { this._Username = value; }
        }

        private BitmapImage _ImageData;
        public BitmapImage ImageData
        {
            get { return this._ImageData; }
            set { this._ImageData = value; }
        }

        public Person() { }

        public Person(string n, string c, string s, string ip, string port)
        {
            t = new System.Timers.Timer(5000);
            this.name = n;
            this.surname = c;
            this.state = s;
            this.imNew = true;
            this.ip = IPAddress.Parse(ip);
            this.port = int.Parse(port);
            t.Elapsed += onTimeElapse;
            t.AutoReset = true;
            t.Start();
        }

        public void reset()
        {
            t.Stop();
            isOld = false;
            t.Start();
        }

        private void onTimeElapse(object sender, System.Timers.ElapsedEventArgs e)
        {
            isOld = true;
            t.Stop();
            t.Start();
            //throw new Exception();
        }
        public string getName()
        {
            return name;
        }

        public void setName(string n)
        {
            this.name = n;
        }

        public string getSurname()
        {
            return this.surname;
        }

        public void setSurname(string c)
        {
            this.surname = c;
        }

        public string getState()
        {
            return this.state;
        }

        public void setState(string s)
        {
            this.state = s;
        }

        public bool isNew()
        {
            // L'utente è una nuova aggiunta?
            return imNew;
        }

        public bool old()
        {
            return isOld;
        }
        public void setOld()
        {
            // L'utente non è più una nuova aggiunta
            imNew = false;
        }

        public IPAddress getIp()
        {
            return ip;
        }

        public int getPort()
        {
            return port;
        }

        public bool isOnline()
        {
            if (state.Equals("online"))
                return true;
            else
                return false;
        }

        public string getString()
        {
            return name + "," + surname + "," + state + "," + ip.ToString() + "," + port;
        }

        public bool isEqual(Person p)
        {
            return (p.getSurname().CompareTo(surname) == 0)
                   && (p.getName().CompareTo(name) == 0)
                   && (p.getIp().ToString().CompareTo(ip.ToString()) == 0)
                   && (p.getPort() == port);
        }
/*
        internal void addButton(MetroTile btn)
        {
            a = btn;
        }

        internal MetroTile getButton()
        {
            return a;
            throw new NotImplementedException();
        }
        */
    }
}
