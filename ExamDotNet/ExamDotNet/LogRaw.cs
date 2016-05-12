using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Esame8
{
    public class LogRaw
    {
        private float _a, _b;
        private string _tipoEvento;
        
        public LogRaw(float a, float b, string tipoEvento)
        {
            this._a = a;
            this._b = b;
            this._tipoEvento = tipoEvento;
        }

        public float a
        {
            get{ return _a; }
            set{ _a = value; }
        }
            
        public float b
        {
            get{ return _b; }
            set{ _b = value; }
        }

        public string tipoEvento
        {
            get{ return _tipoEvento; }
            set{ _tipoEvento = value; }
        }

        public override string ToString()
        {
            return this.a + " " + this.b + " " + this.tipoEvento;
        }
    }
}
