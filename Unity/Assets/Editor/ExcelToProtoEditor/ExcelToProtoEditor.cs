using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ETModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;

public class ExcelToProtoEditor : EditorWindow
{
	[MenuItem("Tools/导出配置Proto")]
	private static void ShowWindow()
	{
		GetWindow(typeof(ExcelToProtoEditor));
	}

	private const string ExcelPath = "../Excel";
	
	// Update is called once per frame
	private void OnGUI()
	{
		try
		{
			if (GUILayout.Button("导出Proto类"))
			{
				ExportAllProtoClass("Assets/Model/Config/", "ETModel");
				ExportAllProtoClass("Assets/Hotfix/Config/", "ETHotfix");
				ExportAllProtoClass("../Server/Model/Config", "ETModel");
			}
			
			if (GUILayout.Button("导出Proto数据到客户端"))
			{
				this.ExportDataAll("./Assets/Res/Config");
			}
			
			if (GUILayout.Button("导出Proto数据到服务端"))
			{
				this.ExportDataAll("../Config");
			}
        }
		catch (Exception e)
		{
			Log.Error(e);
		}
	}

	private void ExportDataAll(string exportDir)
	{
		foreach (string filePath in Directory.GetFiles(ExcelPath))
		{
			if (Path.GetExtension(filePath) != ".xlsx")
			{
				continue;
			}
			
			if (Path.GetFileName(filePath).StartsWith("~"))
			{
				continue;
			}

			this.ExportData(filePath, exportDir);
		}
	}
	
	
	private void ExportData(string filePath, string exportDir)
	{
		string className = Path.GetFileNameWithoutExtension(filePath);
		Type pbCollectionType = typeof (Game).Assembly.GetType("ETModel." + className + "Collection");
		Type pbType = typeof (Game).Assembly.GetType("ETModel." + className);
		object pbCollectionObject = Activator.CreateInstance(pbCollectionType);
		object configs = pbCollectionObject.GetType().GetProperty("Configs").GetValue(pbCollectionObject);
		MethodInfo methodInfo = configs.GetType().GetMethod("Add", new Type[] {pbType});
		
		XSSFWorkbook xssfWorkbook;
		using (FileStream file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{
			xssfWorkbook = new XSSFWorkbook(file);
		}
		
		string exportPath = Path.Combine(exportDir, $"{className}.bytes");
		using (FileStream fileStream = new FileStream(exportPath, FileMode.Create))
		{
			for (int i = 0; i < xssfWorkbook.NumberOfSheets; ++i)
			{
				ISheet sheet = xssfWorkbook.GetSheetAt(i);
				ExportSheet(sheet, configs, methodInfo, pbType);
			}

			byte[] bytes = ProtobufHelper.ToBytes(pbCollectionObject);
			fileStream.Write(bytes, 0, bytes.Length);
		}
	}

	private void ExportSheet(ISheet sheet, object configs, MethodInfo methodInfo, Type pbType)
	{
		int cellCount = sheet.GetRow(3).LastCellNum;

		CellInfo[] cellInfos = new CellInfo[cellCount];

		for (int i = 2; i < cellCount; i++)
		{
			string fieldDesc = GetCellString(sheet, 2, i);
			string fieldName = GetCellString(sheet, 3, i);
			string fieldType = GetCellString(sheet, 4, i);
			cellInfos[i] = new CellInfo() { Name = fieldName, Type = fieldType, Desc = fieldDesc };
		}
		
		for (int i = 5; i <= sheet.LastRowNum; ++i)
		{
			if (GetCellString(sheet, i, 2) == "")
			{
				continue;
			}
			StringBuilder sb = new StringBuilder();
			sb.Append("{");
			IRow row = sheet.GetRow(i);
			
			object pbObject = Activator.CreateInstance(pbType);
			for (int j = 2; j < cellCount; ++j)
			{
				string desc = cellInfos[j].Desc.ToLower();
				if (desc.StartsWith("#"))
				{
					continue;
				}

				string fieldValue = GetCellString(row, j);
				if (fieldValue == "")
				{
					throw new Exception($"sheet: {sheet.SheetName} 中有空白字段 {i},{j}");
				}

				if (j > 2)
				{
					sb.Append(",");
				}

				string fieldName = cellInfos[j].Name;
				string fieldType = cellInfos[j].Type;
				
				SetValue(pbObject, fieldName, fieldType, fieldValue);
			}

			methodInfo.Invoke(configs, new [] { pbObject });
		}
	}

	// 利用excel生成proto文件，然后利用proto文件生成cs文件跟partial文件
	private void ExportAllProtoClass(string exportDir, string ns)
	{
		// 删除之前导出的proto
		string[] protoFiles = Directory.GetFiles(ExcelPath, "*.proto");
		foreach (string protoPath in protoFiles)
		{
			File.Delete(protoPath);
		}

		if (!Directory.Exists(exportDir))
		{
			Directory.CreateDirectory(exportDir);
		}
		
		// 把excel生成proto文件
		foreach (string filePath in Directory.GetFiles(ExcelPath))
		{
			if (Path.GetExtension(filePath) != ".xlsx")
			{
				continue;
			}
			if (Path.GetFileName(filePath).StartsWith("~"))
			{
				continue;
			}

			this.ExportProtoFile(filePath, ns);
			Log.Info($"生成{Path.GetFileName(filePath)}类");
		}
		
		// 把proto生成cs文件
		foreach (string filePath in Directory.GetFiles(ExcelPath))
		{
			if (Path.GetExtension(filePath) != ".proto")
			{
				continue;
			}

			this.ExportProtoCS(Path.GetFileName(filePath), exportDir);
			Log.Info($"生成{Path.GetFileName(filePath)}类");
		}
		
		// 生成protocs的patial类
		foreach (string filePath in Directory.GetFiles(ExcelPath))
		{
			if (Path.GetExtension(filePath) != ".proto")
			{
				continue;
			}

			this.ExportProtoCSPartial(Path.GetFileNameWithoutExtension(filePath), exportDir, ns);
			Log.Info($"生成{Path.GetFileNameWithoutExtension(filePath)}类");
		}
		
		AssetDatabase.Refresh();
	}

	// 生成protocs的patial类
	private void ExportProtoCSPartial(string fileName, string exportDir, string ns)
	{
		string text =
				"using System.Collections.Generic;\n" +
				"using System.ComponentModel\n;" +
				"namespace #ns#\n" +
				"{\n" +
				"    #attribute#\n" + 
				"    public partial class #cls#Collection: ISupportInitialize\n" +
				"    {\n" +
				"        public Dictionary<long, #cls#> configDict = new Dictionary<long, #cls#>();\n" +
				"\n" +
				"        public void BeginInit()\n" +
				"        {\n" +
				"            throw new System.NotImplementedException();\n" +
				"        }\n" +
				"\n" +
				"        public void EndInit()\n" +
				"        {\n" +
				"            foreach (#cls# config in this.Configs)\n" +
				"            {\n" +
				"                this.configDict.Add(config.Id, config);\n" +
				"            }\n" +
				"        }\n" +
				"        public UnitConfig Get(long id)\n" +
				"        {\n" +
				"           this.configDict.TryGetValue(id, out UnitConfig unitConfig);\n" +
				"           return unitConfig;\n" +
				"        }\n" +
				"    }\n" +
				"}\n";
		
		XSSFWorkbook xssfWorkbook;
		using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{
			xssfWorkbook = new XSSFWorkbook(file);
		}

		string attribute = $"[Config({GetCellString(xssfWorkbook.GetSheetAt(0), 0, 0)})]";
		
		string replaceText = text.Replace("#ns#", ns).Replace("#cls#", fileName).Replace("#attribute#", attribute);
		File.WriteAllText($"{exportDir}/{fileName}Collection.cs", replaceText);
	}

	// 把proto生成cs文件
	private void ExportProtoCS(string fileName, string exportDir)
	{
		string protoc = "";
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			protoc = "protoc.exe";
		}
		else
		{
			protoc = "protoc";
		}
		ProcessHelper.Run(protoc, "--csharp_out=\"../Unity/" + exportDir + "\" --proto_path=\"../Excel/\" " + fileName, "../Proto/");
	}
	
	
	// 利用excel生成一个.proto文件
	private void ExportProtoFile(string fileName, string ns)
	{
		XSSFWorkbook xssfWorkbook;
		using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{
			xssfWorkbook = new XSSFWorkbook(file);
		}
		
		string className = Path.GetFileNameWithoutExtension(fileName);

		string exportPath = Path.Combine(ExcelPath, $"{className}.proto");
		
		using (FileStream txt = new FileStream(exportPath, FileMode.Create))
		using (StreamWriter sw = new StreamWriter(txt))
		{
			ISheet sheet = xssfWorkbook.GetSheetAt(0);
			
			StringBuilder sb = new StringBuilder();
			sb.Append("syntax = \"proto3\";\n");
			sb.Append("package " + ns + ";\n\n");
			sb.Append(
				$"message {className}Collection\n" +
				"{\n" +
				$"    repeated {className} Configs = 1;\n" +
				"}\n\n");
			
			sb.Append($"message {className}\n" + "{\n");
			
			int cellCount = sheet.GetRow(3).LastCellNum;
			for (int i = 2; i < cellCount; i++)
			{
				string fieldDesc = GetCellString(sheet, 2, i);

				if (fieldDesc.StartsWith("#"))
				{
					continue;
				}
				
				string fieldName = GetCellString(sheet, 3, i);
				string fieldType = GetCellString(sheet, 4, i);
				if (fieldType == "" || fieldName == "")
				{
					continue;
				}

				sb.Append($"    {GetProtoType(fieldType)} {fieldName} = {i - 1};\n");
			}
			sb.Append("}\n");
			sw.Write(sb.ToString());
		}
	}

	private string GetProtoType(string csType)
	{
		switch (csType)
		{
			case "int":
				return "int32";
			case "long":
				return "int64";
			case "ulong":
				return "uint64";
			case "uint":
				return "uint32";
			case "long[]":
				return "repeated int64";
			case "ulong[]":
				return "repeated uint64";
			case "uint[]":
				return "repeated uint32";
			case "int[]":
				return "repeated int32";
			case "string[]":
				return "repeated string";
			default:
				return csType;
		}
	}

	private static string GetCellString(ISheet sheet, int i, int j)
	{
		return sheet.GetRow(i)?.GetCell(j)?.ToString() ?? "";
	}

	private static string GetCellString(IRow row, int i)
	{
		return row?.GetCell(i)?.ToString() ?? "";
	}

	private static string GetCellString(ICell cell)
	{
		return cell?.ToString() ?? "";
	}

	private static void SetValue(object obj, string fieldName, string fieldType, string value)
	{
		PropertyInfo propertyInfo = obj.GetType().GetProperty(fieldName);
		switch (fieldType)
		{
			case "int":
				propertyInfo.SetValue(obj, int.Parse(value));
				break;
			case "uint":
				propertyInfo.SetValue(obj, uint.Parse(value));
				break;
			case "long":
				propertyInfo.SetValue(obj, long.Parse(value));
				break;
			case "ulong":
				propertyInfo.SetValue(obj, ulong.Parse(value));
				break;
			case "float":
				propertyInfo.SetValue(obj, float.Parse(value));
				break;
			case "double":
				propertyInfo.SetValue(obj, double.Parse(value));
				break;
			case "string":
				propertyInfo.SetValue(obj, value);
				break;
			case "int[]":
			{
				string[] ss = value.Split(',');
				object fieldObj = propertyInfo.GetValue(fieldName);
				MethodInfo methodInfo = fieldObj.GetType().GetMethod("Add");
				foreach (string s in ss)
				{
					int i = int.Parse(s);
					methodInfo.Invoke(fieldObj, new object[] { i });
				}

				break;
			}
			case "uint[]":
			{
				string[] ss = value.Split(',');
				object fieldObj = propertyInfo.GetValue(fieldName);
				MethodInfo methodInfo = fieldObj.GetType().GetMethod("Add");
				foreach (string s in ss)
				{
					uint i = uint.Parse(s);
					methodInfo.Invoke(fieldObj, new object[] { i });
				}

				break;
			}
			case "long[]":
			{
				string[] ss = value.Split(',');
				object fieldObj = propertyInfo.GetValue(fieldName);
				MethodInfo methodInfo = fieldObj.GetType().GetMethod("Add");
				foreach (string s in ss)
				{
					long i = long.Parse(s);
					methodInfo.Invoke(fieldObj, new object[] { i });
				}

				break;
			}
			case "ulong[]":
			{
				string[] ss = value.Split(',');
				object fieldObj = propertyInfo.GetValue(fieldName);
				MethodInfo methodInfo = fieldObj.GetType().GetMethod("Add");
				foreach (string s in ss)
				{
					ulong i = ulong.Parse(s);
					methodInfo.Invoke(fieldObj, new object[] { i });
				}

				break;
			}
			case "float[]":
			{
				string[] ss = value.Split(',');
				object fieldObj = propertyInfo.GetValue(fieldName);
				MethodInfo methodInfo = fieldObj.GetType().GetMethod("Add");
				foreach (string s in ss)
				{
					float i = float.Parse(s);
					methodInfo.Invoke(fieldObj, new object[] { i });
				}

				break;
			}
			case "double[]":
			{
				string[] ss = value.Split(',');
				object fieldObj = propertyInfo.GetValue(fieldName);
				MethodInfo methodInfo = fieldObj.GetType().GetMethod("Add");
				foreach (string s in ss)
				{
					double i = double.Parse(s);
					methodInfo.Invoke(fieldObj, new object[] { i });
				}

				break;
			}
			case "string[]":
			{
				string[] ss = value.Split(',');
				object fieldObj = propertyInfo.GetValue(fieldName);
				MethodInfo methodInfo = fieldObj.GetType().GetMethod("Add");
				foreach (string s in ss)
				{
					methodInfo.Invoke(fieldObj, new object[] { s });
				}

				break;
			}

		}
	}
}
