namespace Honeyslop.Trap.Requests;

public static class StaticExtensions
{
    public static readonly IReadOnlySet<string> Default = Parse(
        "ico,png,jpg,jpeg,jpe,gif,webp,bmp,tif,tiff,svg,svgz,avif,heic,heif,psd,ai,eps," +
        "woff,woff2,ttf,otf,eot," +
        "mp3,mp4,m4a,m4v,aac,avi,mov,mkv,webm,wav,ogg,oga,ogv,flac,wma,wmv,flv,swf,3gp," +
        "pdf,doc,docx,xls,xlsx,ppt,pptx,odt,ods,odp,rtf,epub," +
        "zip,rar,7z,tar,gz,tgz,bz2,tbz2,xz,txz,zst,lz,lz4,lzma,cab," +
        "exe,dll,so,dylib,bin,msi,msp,dmg,iso,img,vhd,vhdx,vmdk,qcow2,apk,ipa,deb,rpm,jar,war,ear,class,pyc,pyo," +
        "sqlite,sqlite3,db,mdb,accdb,dbf,frm,ibd,myd,myi," +
        "pcap,pcapng");

    public static IReadOnlySet<string> Parse(string value)
    {
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.TrimStart('.').ToLowerInvariant())
            .Where(x => x.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
    }
}