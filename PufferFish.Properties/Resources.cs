using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace PufferFish.Properties;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
[DebuggerNonUserCode]
[CompilerGenerated]
internal class Resources
{
	private static ResourceManager resourceMan;

	private static CultureInfo resourceCulture;

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static ResourceManager ResourceManager
	{
		get
		{
			if (resourceMan == null)
			{
				ResourceManager temp = new ResourceManager("PufferFish.Properties.Resources", typeof(Resources).Assembly);
				resourceMan = temp;
			}
			return resourceMan;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	internal static CultureInfo Culture
	{
		get
		{
			return resourceCulture;
		}
		set
		{
			resourceCulture = value;
		}
	}

	internal static byte[] Pesce_Palla_Airbag
	{
		get
		{
			object obj = ResourceManager.GetObject("Pesce_Palla_Airbag", resourceCulture);
			return (byte[])obj;
		}
	}

	internal static byte[] Pesce_Palla_D
	{
		get
		{
			object obj = ResourceManager.GetObject("Pesce_Palla_D", resourceCulture);
			return (byte[])obj;
		}
	}

	internal static byte[] Pesce_Palla_R
	{
		get
		{
			object obj = ResourceManager.GetObject("Pesce_Palla_R", resourceCulture);
			return (byte[])obj;
		}
	}

	internal static byte[] PufferfishV2_0_1_54_A
	{
		get
		{
			object obj = ResourceManager.GetObject("PufferfishV2_0_1_54_A", resourceCulture);
			return (byte[])obj;
		}
	}

	internal static byte[] PufferfishV2_0_1_54_A_RET
	{
		get
		{
			object obj = ResourceManager.GetObject("PufferfishV2_0_1_54_A_RET", resourceCulture);
			return (byte[])obj;
		}
	}

	internal static Bitmap Screenshot_2
	{
		get
		{
			object obj = ResourceManager.GetObject("Screenshot_2", resourceCulture);
			return (Bitmap)obj;
		}
	}

	internal Resources()
	{
	}
}
