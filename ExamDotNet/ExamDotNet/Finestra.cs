using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Esame8
{
    public class Finestra
    {
        private List<Record> _records;
        private List<float> _alTempo;

        public List<Record> records
        {
            get { return _records; }
        }

        public List<float> alTempo
        {
            get { return _alTempo; }
        }

        public Finestra(List<Record> records, List<float> alTempo)
        {
            this._records = records;
            this._alTempo = alTempo;
        }
    }
}
