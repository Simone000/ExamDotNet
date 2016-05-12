using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Esame8
{
    public class Log
    {
        private static StreamWriter file;
        private List<LogRaw> _rows;
        private static float sensibilita = 0.3F; //unisco gli intervalli che si differenziano al max per questa sensibilità

        public List<LogRaw> rows
        {
            get { return _rows; }
        }

        public Log(string txtName)
        {
            file = new StreamWriter(txtName);
            _rows = new List<LogRaw>();
        }

        private static bool FindByString(LogRaw e, string c)
        {
            return (e.tipoEvento.Equals(c));
        }

        public void azzeraLista()
        {
            _rows.Clear();
        }

        public void addLogRawRange(List<LogRaw> logRaws)
        {
            foreach (LogRaw item in logRaws)
            {
                addLogRaw(item);
            }
        }

        public void addLogRaw(LogRaw logRaw)
        {
            _rows.Add(logRaw);
        }

        public void print()
        {
            foreach (LogRaw item in _rows)
            {
                file.WriteLine(item.ToString());
            }
            file.Close();
        }

        public void raggruppaIntervalli(List<LogRaw> righe)
        {
            for (int i = 0; i < righe.Count - 1; i++)
            {
                if (isBetween(righe.ElementAt(i), righe.ElementAt(i + 1))) //se due intervalli dello stesso tipo si intersecano
                {
                    if (righe.ElementAt(i).b <= righe.ElementAt(i + 1).b) //l'intervallo successivo finisce dopo
                    {
                        righe.Insert(i, new LogRaw(righe.ElementAt(i).a, righe.ElementAt(i + 1).b, righe.ElementAt(i).tipoEvento)); //creo un intervallo che li include entrambi
                        righe.RemoveAt(i + 1); //elimino l'elemento attuale
                        righe.RemoveAt(i + 1); //elimino il successivo
                        i--; //forzo il ricontrollo dell'elemento
                    }
                    else if (righe.ElementAt(i).b > righe.ElementAt(i + 1).b) //l'intervallo successivo e' completamente incluso in questo
                    {
                        righe.RemoveAt(i + 1);
                        i--;
                    }
                }
            }
        }

        public bool isBetween(LogRaw x, LogRaw intervallo)
        {
            if (x.tipoEvento.CompareTo(intervallo.tipoEvento) == 0) //se sono dello stesso tipo
                if (x.b + sensibilita >= intervallo.a) //se i 2 intervalli si intersecano (considero anche una sensibilità)
                    return true;
            return false;
        }

        public void riorganizza()
        {
            List<LogRaw> righeStazionarie = new List<LogRaw>();
            List<LogRaw> righeNonStazionarie = new List<LogRaw>();

            foreach (LogRaw item in _rows)  //divido la lista in eventi stazionari e non
            {
                if (item.tipoEvento.CompareTo("stazionario") == 0) //e' di tipo stazionario
                    righeStazionarie.Add(item);
                else
                    righeNonStazionarie.Add(item);
            }

            raggruppaIntervalli(righeStazionarie);
            raggruppaIntervalli(righeNonStazionarie);

            righeStazionarie.AddRange(righeNonStazionarie);

            _rows = righeStazionarie;

            _rows.OrderBy(x => x.a);
        }
    }
}
