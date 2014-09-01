using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UV.Lib.FrontEnds.Utilities
{
	public partial class PropertyForm : Form
	{
		public PropertyForm(object objectWithProperties)
		{
			InitializeComponent();
			this.propertyGrid.SelectedObject = objectWithProperties;
			
			// Create name for this form
			if (objectWithProperties is Form)
				this.Text = ((Form)objectWithProperties).Text + " properties";
			else
			{
				string[] s = objectWithProperties.ToString().Split(new char[]{'.'},StringSplitOptions.RemoveEmptyEntries);
				StringBuilder sb = new StringBuilder();
				string s1 = s[s.Length - 1];			// get last element, usually class name.
				string[] s2 = s1.Split(new char[] { ',',' ' }, StringSplitOptions.RemoveEmptyEntries);
				sb.AppendFormat("{0} properties",s2[0]);
				this.Text = sb.ToString();
			}
		}		
	}
}
