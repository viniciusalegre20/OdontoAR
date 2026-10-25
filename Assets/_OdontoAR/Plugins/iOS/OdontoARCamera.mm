// Permissão de câmera direto do iOS (AVFoundation). Application.HasUserAuthorization não reflete
// a permissão real no iOS, então o app consulta e pede por aqui.
#import <AVFoundation/AVFoundation.h>

extern "C"
{
    // 0 = ainda não perguntado, 1 = restrito, 2 = negado, 3 = permitido (AVAuthorizationStatus).
    int OdontoAR_CameraAuthorizationStatus()
    {
        return (int)[AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo];
    }

    // Mostra o alerta do iOS (só na primeira vez); o resultado é lido depois pelo status.
    void OdontoAR_RequestCameraAccess()
    {
        [AVCaptureDevice requestAccessForMediaType:AVMediaTypeVideo completionHandler:^(BOOL granted) {}];
    }
}
