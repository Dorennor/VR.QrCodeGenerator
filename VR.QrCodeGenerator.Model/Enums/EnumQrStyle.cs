namespace VR.QrCodeGenerator.Model.Enums
{
    /// <summary>
    /// How the QR code is drawn. Custom styles keep the code itself intact and decorate the space around it.
    /// To add one: a value here, an IQrStyle in the Service project registered in QrStyles, and an entry in
    /// CustomCodesViewModel.Styles.
    /// </summary>
    public enum EnumQrStyle
    {
        Classic,
        Kurkod
    }
}
