using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
namespace ProjectDesk;

// macOS adapter for connector and AI secrets: generic-password items (service "Planora") in the user's default
// keychain, accessed through the Security framework so values never appear on a command line.
[SupportedOSPlatform("macos")]
internal static class MacKeychain {
    private const string SecurityFramework="/System/Library/Frameworks/Security.framework/Security";
    private const string CoreFoundationFramework="/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const int Success=0,ItemNotFound=-25300;
    private const string Service="Planora",Label="Planora connector credential";
    private static readonly Lazy<Symbols> Names=new(Symbols.Load);

    public static string? Read(string account){
        var s=Names.Value;using var query=Query(account);
        query.Set(s.ReturnData,s.True);query.Set(s.MatchLimit,s.MatchLimitOne);
        int status=SecItemCopyMatching(query.Handle,out var result);
        if(status==ItemNotFound)return null;
        Check(status,"read");
        try{
            var bytes=new byte[(int)CFDataGetLength(result)];Marshal.Copy(CFDataGetBytePtr(result),bytes,0,bytes.Length);
            try{return Encoding.UTF8.GetString(bytes);}finally{Array.Clear(bytes);}
        }finally{CFRelease(result);}
    }
    public static void Write(string account,string value){
        var s=Names.Value;var bytes=Encoding.UTF8.GetBytes(value);
        try{
            using var data=new CFObject(CFDataCreate(IntPtr.Zero,bytes,bytes.Length));
            using var query=Query(account);using var update=new CFDictionary();update.Set(s.ValueData,data.Handle);
            // Update keeps the existing item and its access control; add only when no item exists yet.
            int status=SecItemUpdate(query.Handle,update.Handle);
            if(status==ItemNotFound){using var label=CFObject.String(Label);query.Set(s.ValueData,data.Handle);query.Set(s.Label,label.Handle);status=SecItemAdd(query.Handle,IntPtr.Zero);}
            Check(status,"store");
        }finally{Array.Clear(bytes);}
    }
    public static void Remove(string account){
        using var query=Query(account);int status=SecItemDelete(query.Handle);
        if(status!=ItemNotFound)Check(status,"remove");
    }
    private static CFDictionary Query(string account){
        var s=Names.Value;var query=new CFDictionary();
        using var service=CFObject.String(Service);using var name=CFObject.String(account);
        query.Set(s.Class,s.GenericPassword);query.Set(s.Service,service.Handle);query.Set(s.Account,name.Handle);
        return query;
    }
    private const int UserCanceled=-128,AuthFailed=-25293,InteractionNotAllowed=-25308;
    private static void Check(int status,string operation){
        if(status==Success)return;
        // Unsigned builds change identity on update, so macOS may ask again and the user can deny access.
        if(status is UserCanceled or AuthFailed or InteractionNotAllowed)throw new IOException($"Planora was not allowed to {operation} the credential in the macOS Keychain. Allow access when macOS asks, then try again.");
        throw new IOException($"Could not {operation} the credential in the macOS Keychain (OSStatus {status}).");
    }

    private sealed record Symbols(IntPtr Class,IntPtr GenericPassword,IntPtr Service,IntPtr Account,IntPtr ValueData,IntPtr ReturnData,IntPtr MatchLimit,IntPtr MatchLimitOne,IntPtr Label,IntPtr True,IntPtr KeyCallBacks,IntPtr ValueCallBacks){
        public static Symbols Load(){
            var security=NativeLibrary.Load(SecurityFramework);var foundation=NativeLibrary.Load(CoreFoundationFramework);
            IntPtr Constant(IntPtr library,string name)=>Marshal.ReadIntPtr(NativeLibrary.GetExport(library,name));
            return new(Constant(security,"kSecClass"),Constant(security,"kSecClassGenericPassword"),Constant(security,"kSecAttrService"),Constant(security,"kSecAttrAccount"),Constant(security,"kSecValueData"),Constant(security,"kSecReturnData"),Constant(security,"kSecMatchLimit"),Constant(security,"kSecMatchLimitOne"),Constant(security,"kSecAttrLabel"),Constant(foundation,"kCFBooleanTrue"),NativeLibrary.GetExport(foundation,"kCFTypeDictionaryKeyCallBacks"),NativeLibrary.GetExport(foundation,"kCFTypeDictionaryValueCallBacks"));
        }
    }
    private class CFObject(IntPtr handle):IDisposable{
        public IntPtr Handle{get;}=handle!=IntPtr.Zero?handle:throw new IOException("Core Foundation allocation failed.");
        public static CFObject String(string value)=>new(CFStringCreateWithCharacters(IntPtr.Zero,value,value.Length));
        public void Dispose()=>CFRelease(Handle);
    }
    private sealed class CFDictionary():CFObject(CFDictionaryCreateMutable(IntPtr.Zero,0,Names.Value.KeyCallBacks,Names.Value.ValueCallBacks)){
        public void Set(IntPtr key,IntPtr value)=>CFDictionarySetValue(Handle,key,value);
    }

    [DllImport(SecurityFramework)]private static extern int SecItemAdd(IntPtr attributes,IntPtr result);
    [DllImport(SecurityFramework)]private static extern int SecItemCopyMatching(IntPtr query,out IntPtr result);
    [DllImport(SecurityFramework)]private static extern int SecItemUpdate(IntPtr query,IntPtr attributesToUpdate);
    [DllImport(SecurityFramework)]private static extern int SecItemDelete(IntPtr query);
    [DllImport(CoreFoundationFramework,CharSet=CharSet.Unicode)]private static extern IntPtr CFStringCreateWithCharacters(IntPtr allocator,string characters,nint length);
    [DllImport(CoreFoundationFramework)]private static extern IntPtr CFDataCreate(IntPtr allocator,byte[] bytes,nint length);
    [DllImport(CoreFoundationFramework)]private static extern nint CFDataGetLength(IntPtr data);
    [DllImport(CoreFoundationFramework)]private static extern IntPtr CFDataGetBytePtr(IntPtr data);
    [DllImport(CoreFoundationFramework)]private static extern IntPtr CFDictionaryCreateMutable(IntPtr allocator,nint capacity,IntPtr keyCallBacks,IntPtr valueCallBacks);
    [DllImport(CoreFoundationFramework)]private static extern void CFDictionarySetValue(IntPtr dictionary,IntPtr key,IntPtr value);
    [DllImport(CoreFoundationFramework)]private static extern void CFRelease(IntPtr value);
}
