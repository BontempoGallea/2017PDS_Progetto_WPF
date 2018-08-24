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
        private string _Name;
        private string _Surname;
        private bool _State;
        private string _Username;


        private IPAddress ip;
        private int _Port;

        private bool isOld;
        private bool imNew;
        private System.Timers.Timer t;

        public Person() { }

        public Person(string n, string c, bool s, string ip, int port)
        {
            t = new System.Timers.Timer(5000);
            this._Name = n;
            this._Surname = c;
            this._State = s;
            this.imNew = true;
            this.ip = IPAddress.Parse(ip);
            this._Port = port;
            t.Elapsed += OnTimeElapse;
            t.AutoReset = true;
            t.Start();
            _imageData = new BitmapImage(new Uri("pack://application:,,,/img.jpg"));
            _imageData.Freeze();
        }

        public string Username
        {
            get { return this._Name+" "+ this._Surname; }
            set { this._Username = value; }
        }

        public string Name
        {
            get { return this._Name; }
            set { this._Name = value; }
        }

        public string Surname
        {
            get { return this._Surname; }
            set { this._Surname = value; }
        }

        public bool State
        {
            get { return this._State; }
            set { this._State = value; }
        }

        public int Port
        {
            get { return this._Port; }
            set { this._Port = value; }
        }

        private BitmapImage _imageData;
        public BitmapImage ImageData
        {
            get
            {
                return this._imageData;
            }
            set { this._imageData = value; }
        }

        public IPAddress GetIp()
        {
            return ip;
        }

        public string GetString()
        {
            return _Name + "," + _Surname + "," + this.GetStateAsString() + "," + ip.ToString() + "," + _Port;
        }

        private string GetStateAsString()
        {
            return (this._State) ? "online" : "offline";
        }

        public bool IsEqual(Person p)
        {
            return (p.Surname.CompareTo(this._Surname) == 0)
                   && (p.Name.CompareTo(this._Name) == 0)
                   && (p.GetIp().ToString().CompareTo(ip.ToString()) == 0)
                   && (p.Port == this._Port);
        }

        public bool IsOnline()
        {
            return this._State;
        }

        public void Reset()
        {
            t.Stop();
            isOld = false;
            t.Start();
        }

        private void OnTimeElapse(object sender, System.Timers.ElapsedEventArgs e)
        {
            isOld = true;
            t.Stop();
            t.Start();
            //throw new Exception();
        }

        public bool IsNew()
        {
            // L'utente è una nuova aggiunta?
            return imNew;
        }

        public bool Old()
        {
            return isOld;
        }
        public void SetOld()
        {
            // L'utente non è più una nuova aggiunta
            imNew = false;
        }
    }
}
