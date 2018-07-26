using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCondivisione
{
    class SharedVariables
    {
        private static Boolean _closeEverything = false; // Questo è il flag al quale i thread fanno riferimento per sapere se devono chiudere tutto o no
        private static string _pathSend = "C:\\Users\\host1\\Documents\\catia.zip"; // Path del file / della cartella da inviare
        private static string _pathSave = @"C:\Users\" + Environment.UserName + @"\Downloads"; // Path di default per il salvataggio dei files in arrivo
        private static ListUserHandler _luh = new ListUserHandler();
        private static bool _automaticSave = true; // True = non popparmi la finestra di accetazione quando mi arriva un file  
        public static bool _annullaBoolean = false;
        public static MainWindow W;
        public static bool CloseEverything
        {
            get { return _closeEverything; }
            set { _closeEverything = value; }
        }

        public static string PathSend
        {
            get { return _pathSend; }
            set { lock (_pathSend) { _pathSend = value; } }
        }

        public static string PathSave
        {
            get { return _pathSave; }
            set { lock(_pathSave) { _pathSave = value; } }
        }

        public static ListUserHandler Luh
        {
            get { return _luh; }
        }

        public static bool AutomaticSave
        {
            get { return _automaticSave; }
            set { _automaticSave = value; }
        }

        public static bool Annulla
        {
            get { return _annullaBoolean; }
            set { _annullaBoolean = value; }
        }
    }
}
