/// <summary>
/// Backend storage để chọn trong Unity Inspector.
/// </summary>
public enum SaveBackend
{
    /// <summary>Easy Save 3 (AES mã hóa). Khuyến nghị cho production.</summary>
    ES3  = 0,

    /// <summary>JSON text files thuần. Không mã hóa, dễ debug trên Editor.</summary>
    Json = 1
}
