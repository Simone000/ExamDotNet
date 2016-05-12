using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Esame8
{
    public class Sensore
    {
        private List<Record> records;
        private List<float> alTempo;
        private float avanzamentoTempo;
        private float contatore;
        private int numeroRecordGiaPresi;

        public Sensore(float avanzamentoTempo)
        {
            records = new List<Record>();
            alTempo = new List<float>();
            contatore = 0;
            numeroRecordGiaPresi = 0;
            this.avanzamentoTempo = avanzamentoTempo;
        }

        public void addRecord(Record record)
        {
            this.records.Add(record);
            this.alTempo.Add(contatore*avanzamentoTempo);
            this.contatore = contatore + 1;
        }

        public Finestra getFinestra(int numeroRecord)
        {
            Finestra risultato = new Finestra(records.GetRange(numeroRecordGiaPresi, numeroRecord), alTempo.GetRange(numeroRecordGiaPresi, numeroRecord));
            this.numeroRecordGiaPresi = numeroRecordGiaPresi + numeroRecord;
            return risultato;
        }

        public Record getLastRecord()
        {
            return this.records.Last();
        }
    }
}
