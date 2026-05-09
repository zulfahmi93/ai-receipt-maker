import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:provider/provider.dart';
import 'package:receipt_demo_flutter/services/receipt_api_client.dart';
import 'package:receipt_demo_flutter/services/receipt_share_service.dart';
import 'package:receipt_demo_flutter/state/receipt_state.dart';
import 'package:receipt_demo_flutter/widgets/receipt_actions.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  final baseUrl = Uri.parse('https://receipt-toolkit.test');

  group('ReceiptActions', () {
    testWidgets(
      'T7.13 Share PDF generates PDF bytes and passes them to the share service',
      (tester) async {
        const receiptJson = '{"schemaVersion":1}';
        final pdfBytes = Uint8List.fromList(<int>[37, 80, 68, 70]);
        final shareService = _FakeReceiptShareService();
        final state = ReceiptState(
          apiClient: ReceiptApiClient(
            baseUrl: baseUrl,
            httpClient: MockClient((request) async {
              expect(request.method, 'POST');
              expect(request.url, baseUrl.resolve('/api/receipts/pdf'));
              expect(request.body, receiptJson);

              return http.Response.bytes(
                pdfBytes,
                200,
                headers: {'content-type': 'application/pdf'},
              );
            }),
          ),
          initialJsonText: receiptJson,
        );

        await tester.pumpWidget(_hostedActions(state, shareService));
        await tester.tap(find.byKey(const Key('share-pdf')));
        await tester.pumpAndSettle();

        expect(shareService.sharedPdfBytes, orderedEquals(pdfBytes));
        expect(shareService.sharedPngBytes, isNull);
      },
    );

    testWidgets(
      'T7.13 Share PNG uses current preview bytes when already generated',
      (tester) async {
        const receiptJson = '{"schemaVersion":1}';
        final pngBytes = Uint8List.fromList(<int>[137, 80, 78, 71]);
        final shareService = _FakeReceiptShareService();
        final state = ReceiptState(
          apiClient: ReceiptApiClient(
            baseUrl: baseUrl,
            httpClient: MockClient((request) async {
              expect(request.method, 'POST');
              expect(request.url, baseUrl.resolve('/api/receipts/png'));
              expect(request.body, receiptJson);

              return http.Response.bytes(
                pngBytes,
                200,
                headers: {'content-type': 'image/png'},
              );
            }),
          ),
          initialJsonText: receiptJson,
        );
        await state.regenerate();

        await tester.pumpWidget(_hostedActions(state, shareService));
        await tester.tap(find.byKey(const Key('share-png')));
        await tester.pumpAndSettle();

        expect(shareService.sharedPngBytes, orderedEquals(pngBytes));
        expect(shareService.sharedPdfBytes, isNull);
      },
    );

    testWidgets(
      'T7.14 Reset sample restores bundled JSON and regenerates preview bytes',
      (tester) async {
        final previewBytes = Uint8List.fromList(<int>[137, 80, 78, 71, 13]);
        final shareService = _FakeReceiptShareService();
        final state = ReceiptState(
          apiClient: ReceiptApiClient(
            baseUrl: baseUrl,
            httpClient: MockClient((request) async {
              expect(request.method, 'POST');
              expect(request.url, baseUrl.resolve('/api/receipts/png'));

              final body = jsonDecode(request.body) as Map<String, dynamic>;
              expect(
                body['business'],
                containsPair('businessName', 'Elevate Studio'),
              );
              expect(body['theme'], containsPair('accentColor', '#3F6F63'));

              return http.Response.bytes(
                previewBytes,
                200,
                headers: {'content-type': 'image/png'},
              );
            }),
          ),
          initialJsonText: jsonEncode({
            'schemaVersion': 1,
            'business': {'businessName': 'Changed Business'},
            'theme': {'accentColor': '#C83232'},
          }),
        );

        await tester.pumpWidget(_hostedActions(state, shareService));
        await tester.tap(find.byKey(const Key('reset-sample')));
        await tester.pumpAndSettle();

        final json = jsonDecode(state.jsonText) as Map<String, dynamic>;
        expect(
          json['business'],
          containsPair('businessName', 'Elevate Studio'),
        );
        expect(json['theme'], containsPair('accentColor', '#3F6F63'));
        expect(state.previewBytes, orderedEquals(previewBytes));
      },
    );
  });
}

Widget _hostedActions(ReceiptState state, ReceiptShareService shareService) {
  return ChangeNotifierProvider<ReceiptState>.value(
    value: state,
    child: MaterialApp(
      home: Scaffold(body: ReceiptActions(shareService: shareService)),
    ),
  );
}

class _FakeReceiptShareService implements ReceiptShareService {
  Uint8List? sharedPdfBytes;
  Uint8List? sharedPngBytes;

  @override
  Future<void> sharePdf(Uint8List bytes) async {
    sharedPdfBytes = bytes;
  }

  @override
  Future<void> sharePng(Uint8List bytes) async {
    sharedPngBytes = bytes;
  }
}
