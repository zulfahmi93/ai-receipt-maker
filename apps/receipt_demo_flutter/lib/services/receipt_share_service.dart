import 'dart:typed_data';

import 'package:share_plus/share_plus.dart';

abstract interface class ReceiptShareService {
  Future<void> sharePdf(Uint8List bytes);

  Future<void> sharePng(Uint8List bytes);
}

class DefaultReceiptShareService implements ReceiptShareService {
  const DefaultReceiptShareService();

  @override
  Future<void> sharePdf(Uint8List bytes) {
    return SharePlus.instance.share(
      ShareParams(
        files: [
          XFile.fromData(
            bytes,
            mimeType: 'application/pdf',
            name: 'receipt.pdf',
          ),
        ],
        fileNameOverrides: const ['receipt.pdf'],
      ),
    );
  }

  @override
  Future<void> sharePng(Uint8List bytes) {
    return SharePlus.instance.share(
      ShareParams(
        files: [
          XFile.fromData(bytes, mimeType: 'image/png', name: 'receipt.png'),
        ],
        fileNameOverrides: const ['receipt.png'],
      ),
    );
  }
}
