import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:receipt_demo_flutter/services/receipt_api_client.dart';
import 'package:receipt_demo_flutter/state/receipt_state.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  final baseUrl = Uri.parse('https://receipt-toolkit.test');

  group('ReceiptState', () {
    test(
      'T7.5 loadSample populates JSON from the bundled sample asset',
      () async {
        final state = ReceiptState(
          apiClient: ReceiptApiClient(
            baseUrl: baseUrl,
            httpClient: MockClient((_) async => http.Response('', 500)),
          ),
        );
        var notifyCount = 0;
        state.addListener(() => notifyCount++);

        await state.loadSample();

        final json = _decodeReceiptJson(state.jsonText);
        expect(json['schemaVersion'], 1);
        expect(
          json['business'],
          containsPair('businessName', 'Elevate Studio'),
        );
        expect(json['theme'], containsPair('accentColor', '#3F6F63'));
        expect(json['options'], containsPair('showQrCode', true));
        expect(notifyCount, 1);
      },
    );

    test('T7.6 regenerate calls the API and assigns previewBytes', () async {
      const receiptJson = '{"schemaVersion":1}';
      final expectedBytes = Uint8List.fromList(<int>[137, 80, 78, 71]);
      var apiWasCalled = false;
      var notifyCount = 0;
      final state = ReceiptState(
        apiClient: ReceiptApiClient(
          baseUrl: baseUrl,
          httpClient: MockClient((request) async {
            apiWasCalled = true;
            expect(request.method, 'POST');
            expect(request.url, baseUrl.resolve('/api/receipts/png'));
            expect(request.body, receiptJson);

            return http.Response.bytes(
              expectedBytes,
              200,
              headers: {'content-type': 'image/png'},
            );
          }),
        ),
        initialJsonText: receiptJson,
      )..addListener(() => notifyCount++);

      await state.regenerate();

      expect(apiWasCalled, isTrue);
      expect(state.previewBytes, expectedBytes);
      expect(state.errorMessage, isNull);
      expect(state.isLoading, isFalse);
      expect(notifyCount, 2);
    });

    test(
      'regenerate clears stale preview bytes and exposes API errors',
      () async {
        const receiptJson = '{"schemaVersion":1}';
        var requestCount = 0;
        final state = ReceiptState(
          apiClient: ReceiptApiClient(
            baseUrl: baseUrl,
            httpClient: MockClient((_) async {
              requestCount++;
              if (requestCount == 1) {
                return http.Response.bytes(
                  Uint8List.fromList(<int>[137, 80, 78, 71]),
                  200,
                  headers: {'content-type': 'image/png'},
                );
              }

              return http.Response(
                jsonEncode({
                  'title': 'Receipt generation failed.',
                  'status': 500,
                  'detail': 'Renderer failed.',
                }),
                500,
                headers: {'content-type': 'application/problem+json'},
              );
            }),
          ),
          initialJsonText: receiptJson,
        );

        await state.regenerate();
        expect(state.previewBytes, isNotNull);

        await state.regenerate();

        expect(state.previewBytes, isNull);
        expect(state.errorMessage, contains('Receipt generation failed.'));
        expect(state.isLoading, isFalse);
      },
    );

    test(
      'T7.7 updateAccentColor mutates theme.accentColor and notifies listeners',
      () {
        var notifyCount = 0;
        final state = ReceiptState(
          apiClient: ReceiptApiClient(
            baseUrl: baseUrl,
            httpClient: MockClient((_) async => http.Response('', 500)),
          ),
          initialJsonText: jsonEncode({
            'schemaVersion': 1,
            'theme': {'accentColor': '#3F6F63'},
          }),
        )..addListener(() => notifyCount++);

        state.updateAccentColor('#C83232');

        final json = _decodeReceiptJson(state.jsonText);
        expect(json['theme'], containsPair('accentColor', '#C83232'));
        expect(notifyCount, 1);
      },
    );

    test('structured mutations require an existing JSON object', () {
      var notifyCount = 0;
      final state = ReceiptState(
        apiClient: ReceiptApiClient(
          baseUrl: baseUrl,
          httpClient: MockClient((_) async => http.Response('', 500)),
        ),
      )..addListener(() => notifyCount++);

      expect(() => state.updateAccentColor('#C83232'), throwsStateError);
      expect(state.jsonText, isEmpty);
      expect(notifyCount, 0);
    });

    test('T7.8 toggleQr(false) sets options.showQrCode=false and notifies', () {
      var notifyCount = 0;
      final state = ReceiptState(
        apiClient: ReceiptApiClient(
          baseUrl: baseUrl,
          httpClient: MockClient((_) async => http.Response('', 500)),
        ),
        initialJsonText: jsonEncode({
          'schemaVersion': 1,
          'options': {'showQrCode': true},
        }),
      )..addListener(() => notifyCount++);

      state.toggleQr(false);

      final json = _decodeReceiptJson(state.jsonText);
      expect(json['options'], containsPair('showQrCode', false));
      expect(notifyCount, 1);
    });
  });
}

Map<String, dynamic> _decodeReceiptJson(String receiptJson) {
  return jsonDecode(receiptJson) as Map<String, dynamic>;
}
