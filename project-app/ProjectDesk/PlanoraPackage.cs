using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
namespace ProjectDesk;

// Versioned portable model package. Presentation metadata is not yet part of this format.
public static class PlanoraPackage {
 private const int BaseLimit=64*1024*1024,HistoryLimit=16*1024*1024;
 private sealed record Manifest(int SchemaVersion,string BaseSha256,string StateSha256);
 public static void WriteNew(string path,ProjectSession session)=>WriteNew(path,session.CaptureState());
 public static void WriteNew(string path,DurableProjectState state){
  byte[] history=JsonSerializer.SerializeToUtf8Bytes(state with{BaseBytes=[]},DurableCommand.Json);
  if(state.BaseBytes.Length>BaseLimit||history.Length>HistoryLimit)throw new InvalidDataException("Project package exceeds supported size.");
  string destination=Path.GetFullPath(path),temporary=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{
   using(var file=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
    using(var archive=new ZipArchive(file,ZipArchiveMode.Create,true)){
     Put(archive,"base.bin",state.BaseBytes);Put(archive,"state.json",history);
     Put(archive,"manifest.json",JsonSerializer.SerializeToUtf8Bytes(new Manifest(1,Hash(state.BaseBytes),Hash(history))));
    }
    file.Flush(true);
   }
   File.Move(temporary,destination,false);
  }finally{if(File.Exists(temporary))File.Delete(temporary);}
 }
 public static DurableProjectState Read(string path){
  using var file=File.OpenRead(path);using var archive=new ZipArchive(file,ZipArchiveMode.Read);
  string[] expected=["base.bin","state.json","manifest.json"];
  if(archive.Entries.Count!=3||expected.Any(name=>archive.Entries.Count(e=>e.FullName==name)!=1))throw new InvalidDataException("Invalid project package entries.");
  var manifest=JsonSerializer.Deserialize<Manifest>(ReadEntry(archive,"manifest.json",4096))??throw new InvalidDataException("Missing manifest.");
  if(manifest.SchemaVersion!=1)throw new InvalidDataException("Unsupported project package version.");
  var original=ReadEntry(archive,"base.bin",BaseLimit);var history=ReadEntry(archive,"state.json",HistoryLimit);
  if(Hash(original)!=manifest.BaseSha256||Hash(history)!=manifest.StateSha256)throw new InvalidDataException("Project package integrity check failed.");
  var state=JsonSerializer.Deserialize<DurableProjectState>(history,DurableCommand.Json)??throw new InvalidDataException("Missing project state.");
  if(state.BaseBytes==null||state.BaseBytes.Length!=0)throw new InvalidDataException("Unexpected embedded base data.");
  return state with{BaseBytes=original};
 }
 private static string Hash(byte[] bytes)=>Convert.ToHexString(SHA256.HashData(bytes));
 private static void Put(ZipArchive archive,string name,byte[] data){using var stream=archive.CreateEntry(name,CompressionLevel.Optimal).Open();stream.Write(data);}
 private static byte[] ReadEntry(ZipArchive archive,string name,int limit){
  var entry=archive.GetEntry(name)!;if(entry.Length<1||entry.Length>limit)throw new InvalidDataException("Project entry exceeds supported size.");
  using var stream=entry.Open();using var output=new MemoryStream();byte[] buffer=new byte[8192];int count;
  while((count=stream.Read(buffer))!=0){if(output.Length+count>limit)throw new InvalidDataException("Project entry exceeds supported size.");output.Write(buffer,0,count);}
  if(output.Length!=entry.Length)throw new InvalidDataException("Truncated project entry.");return output.ToArray();
 }
}
