using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Esame8
{
    public class Dato
    {
        private float x, y, z;

        public Dato(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public List<float> getDato()
        {
            List<float> ris = new List<float>();
            ris.Add(x);
            ris.Add(y);
            ris.Add(z);
            return ris;
        }

        public float getModulo()
        {
            return (float)(Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2)));
        }

        public float getTheta()
        {
            return (float)Math.Atan(this.z / this.x);
        }
    }
}
