// Folha de compartilhamento do iOS (AirDrop, Arquivos, Mail...) para exportar o registro de uso.
#import <UIKit/UIKit.h>
#import "UnityInterface.h"

extern "C"
{
    void OdontoAR_ShareFile(const char* path)
    {
        NSURL* url = [NSURL fileURLWithPath:[NSString stringWithUTF8String:path]];
        UIActivityViewController* sheet = [[UIActivityViewController alloc] initWithActivityItems:@[url]
                                                                            applicationActivities:nil];
        UIViewController* root = UnityGetGLViewController();

        // No iPad a folha é um popover e precisa de uma âncora.
        sheet.popoverPresentationController.sourceView = root.view;
        sheet.popoverPresentationController.sourceRect =
            CGRectMake(root.view.bounds.size.width / 2, root.view.bounds.size.height - 80, 1, 1);

        [root presentViewController:sheet animated:YES completion:nil];
    }
}
