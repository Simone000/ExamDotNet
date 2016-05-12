using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Esame8
{
    public class Record
    {
        private List<float> dati;
        private static int bytePerDato = 4, numeroDati = 13;

        public Record(List<byte> range)
        {
            dati = new List<float>();
            List<byte> utile = new List<byte>();

            int contatore = 0;
            while (contatore <= (numeroDati - 1) * bytePerDato)
            {
                utile = range.GetRange(contatore, bytePerDato);
                utile.Reverse();
                dati.Add(BitConverter.ToSingle(utile.ToArray(), 0));
                contatore = contatore + bytePerDato;
            }
        }

        public Dato getAccellerazione()
        {
            return new Dato(dati[0], dati[1], dati[2]);
        }

        public Dato getGiroscopio()
        {
            return new Dato(dati[3], dati[4], dati[5]);
        }

        public Dato getMagnetometro()
        {
            return new Dato(dati[6], dati[7], dati[8]);
        }

        public float getDatoAtPos(int posizione)
        {
            return this.dati.ElementAt(posizione);
        }

        public void stampa(SmartXLS.WorkBook foglio, int riga, int colonna)
        {
            foreach (float item in dati)
            {
                foglio.setText(riga, colonna, item.ToString());
                colonna++;
            }
        }
    }
}
