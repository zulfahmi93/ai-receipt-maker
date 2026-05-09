import 'dart:typed_data';

import 'package:flutter/widgets.dart';

class ReceiptPreview extends StatelessWidget {
  const ReceiptPreview({super.key, required this.previewBytes});

  final Uint8List? previewBytes;

  @override
  Widget build(BuildContext context) {
    final bytes = previewBytes;
    if (bytes == null) {
      return const SizedBox(key: Key('receipt-preview-placeholder'));
    }

    return Image.memory(bytes, key: const Key('receipt-preview-image'));
  }
}
