using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCondivisione
{
    static class SharedVariables
    {
        private static Boolean _CloseEverything = false; // Questo è il flag al quale i thread fanno riferimento per sapere se devono chiudere tutto o no
        private static string _PathSend = "C:\\Users\\host1\\Documents\\catia.zip"; // Path del file / della cartella da inviare
        private static string _PathSave = @"C:\Users\" + Environment.UserName + @"\Downloads"; // Path di default per il salvataggio dei files in arrivo
        private static ListUserHandler _Luh = new ListUserHandler();
        private static bool _AutomaticSave = true; // True = non popparmi la finestra di accetazione quando mi arriva un file  
        public static bool _AnnullaBoolean = false;
        public static long Uploaded = 0;
        public static long TottoSend = 0;
        public static MainWindow W;
        internal static long fileDimension=0;
        internal static long numberOfDestination=0;

        public static bool CloseEverything
        {
            get { return _CloseEverything; }
            set { _CloseEverything = value; }
        }

        public static string PathSend
        {
            get { return _PathSend; }
            set { lock (_PathSend) { _PathSend = value; } }
        }

        public static string PathSave
        {
            get { return _PathSave; }
            set { lock(_PathSave) { _PathSave = value; } }
        }

        public static ListUserHandler Luh
        {
            get { return _Luh; }
        }

        public static bool AutomaticSave
        {
            get { return _AutomaticSave; }
            set { _AutomaticSave = value; }
        }

        public static bool Annulla
        {
            get { return _AnnullaBoolean; }
            set { _AnnullaBoolean = value; }
        }
    }
}
