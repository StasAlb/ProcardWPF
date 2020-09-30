using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProcardWPF
{
    public class DrawPanel : Panel
    {
        private VisualCollection visuals;
        public DrawPanel() : base()
        {
            visuals = new VisualCollection(this);
        }
        protected override Visual GetVisualChild(int index)
        {
            return visuals[index];
        }
        protected override int VisualChildrenCount
        {
            get
            {
                return visuals.Count;
            }
        }
        public void AddVisual(Visual visual)
        {
            visuals.Add(visual);
        }
        public void ClearVisuals()
        {
            visuals.Clear();
        }
    }
}
