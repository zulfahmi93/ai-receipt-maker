import 'dart:typed_data';

import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:receipt_demo_flutter/widgets/receipt_preview.dart';

void main() {
  group('ReceiptPreview', () {
    testWidgets('T7.12 renders Image.memory when preview bytes are present', (
      tester,
    ) async {
      final previewBytes = Uint8List.fromList(_transparentPngBytes);

      await tester.pumpWidget(
        Directionality(
          textDirection: TextDirection.ltr,
          child: ReceiptPreview(previewBytes: previewBytes),
        ),
      );

      final image = tester.widget<Image>(
        find.byKey(const Key('receipt-preview-image')),
      );
      expect(image.image, isA<MemoryImage>());
      expect((image.image as MemoryImage).bytes, orderedEquals(previewBytes));
      expect(
        find.byKey(const Key('receipt-preview-placeholder')),
        findsNothing,
      );
    });

    testWidgets('T7.12 shows a placeholder when preview bytes are null', (
      tester,
    ) async {
      await tester.pumpWidget(
        const Directionality(
          textDirection: TextDirection.ltr,
          child: ReceiptPreview(previewBytes: null),
        ),
      );

      expect(find.byType(Image), findsNothing);
      expect(
        find.byKey(const Key('receipt-preview-placeholder')),
        findsOneWidget,
      );
    });
  });
}

const _transparentPngBytes = <int>[
  0x89,
  0x50,
  0x4e,
  0x47,
  0x0d,
  0x0a,
  0x1a,
  0x0a,
  0x00,
  0x00,
  0x00,
  0x0d,
  0x49,
  0x48,
  0x44,
  0x52,
  0x00,
  0x00,
  0x00,
  0x01,
  0x00,
  0x00,
  0x00,
  0x01,
  0x08,
  0x06,
  0x00,
  0x00,
  0x00,
  0x1f,
  0x15,
  0xc4,
  0x89,
  0x00,
  0x00,
  0x00,
  0x0a,
  0x49,
  0x44,
  0x41,
  0x54,
  0x78,
  0x9c,
  0x63,
  0x00,
  0x01,
  0x00,
  0x00,
  0x05,
  0x00,
  0x01,
  0x0d,
  0x0a,
  0x2d,
  0xb4,
  0x00,
  0x00,
  0x00,
  0x00,
  0x49,
  0x45,
  0x4e,
  0x44,
  0xae,
  0x42,
  0x60,
  0x82,
];
