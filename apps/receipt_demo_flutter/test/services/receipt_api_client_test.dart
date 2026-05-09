import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:receipt_demo_flutter/services/receipt_api_client.dart';

void main() {
  const receiptJson = '{"schemaVersion":1}';
  final baseUrl = Uri.parse('https://receipt-toolkit.test');

  group('ReceiptApiClient.validate', () {
    test('T7.1 parses 200 success body into a valid typed result', () async {
      final client = ReceiptApiClient(
        baseUrl: baseUrl,
        httpClient: MockClient((request) async {
          expect(request.method, 'POST');
          expect(request.url, baseUrl.resolve('/api/receipts/validate'));
          expect(request.headers['content-type'], contains('application/json'));
          expect(request.body, receiptJson);

          return http.Response(
            jsonEncode({'valid': true, 'errors': <Object>[]}),
            200,
            headers: {'content-type': 'application/json; charset=utf-8'},
          );
        }),
      );

      final result = await client.validate(receiptJson);

      expect(result, isA<ReceiptValidationResult>());
      expect(result.valid, isTrue);
      expect(result.errors, isEmpty);
    });

    test('T7.2 parses invalid validation body into typed errors', () async {
      final client = ReceiptApiClient(
        baseUrl: baseUrl,
        httpClient: MockClient((request) async {
          expect(request.method, 'POST');
          expect(request.url, baseUrl.resolve('/api/receipts/validate'));
          expect(request.headers['content-type'], contains('application/json'));
          expect(request.body, receiptJson);

          return http.Response(
            jsonEncode({
              'valid': false,
              'errors': [
                {
                  'field': 'business.businessName',
                  'message': 'Business name is required.',
                },
              ],
            }),
            200,
            headers: {'content-type': 'application/json; charset=utf-8'},
          );
        }),
      );

      final result = await client.validate(receiptJson);

      expect(result, isA<ReceiptValidationResult>());
      expect(result.valid, isFalse);
      expect(result.errors, hasLength(1));
      expect(result.errors.single, isA<ReceiptValidationError>());
      expect(result.errors.single.field, 'business.businessName');
      expect(result.errors.single.message, 'Business name is required.');
    });

    test(
      'throws ReceiptApiException for malformed JSON ProblemDetails',
      () async {
        final client = ReceiptApiClient(
          baseUrl: baseUrl,
          httpClient: MockClient((request) async {
            expect(request.method, 'POST');
            expect(request.url, baseUrl.resolve('/api/receipts/validate'));

            return http.Response(
              jsonEncode({
                'title': 'Malformed JSON request body.',
                'status': 400,
                'detail': r"'schemaVersion' was not valid JSON.",
              }),
              400,
              headers: {'content-type': 'application/problem+json'},
            );
          }),
        );

        await expectLater(
          client.validate(receiptJson),
          throwsA(
            isA<ReceiptApiException>()
                .having((error) => error.statusCode, 'statusCode', 400)
                .having(
                  (error) => error.message,
                  'message',
                  contains('Malformed JSON request body.'),
                ),
          ),
        );
      },
    );
  });

  group('ReceiptApiClient generation', () {
    test(
      'T7.3 generatePng posts receipt JSON and returns response bytes',
      () async {
        final expectedBytes = Uint8List.fromList(<int>[137, 80, 78, 71]);
        final client = ReceiptApiClient(
          baseUrl: baseUrl,
          httpClient: MockClient((request) async {
            expect(request.method, 'POST');
            expect(request.url, baseUrl.resolve('/api/receipts/png'));
            expect(
              request.headers['content-type'],
              contains('application/json'),
            );
            expect(request.body, receiptJson);

            return http.Response.bytes(
              expectedBytes,
              200,
              headers: {'content-type': 'image/png'},
            );
          }),
        );

        final bytes = await client.generatePng(receiptJson);

        expect(bytes, isA<Uint8List>());
        expect(bytes, expectedBytes);
      },
    );

    test(
      'T7.4 generatePdf posts receipt JSON and returns response bytes',
      () async {
        final expectedBytes = Uint8List.fromList(<int>[37, 80, 68, 70]);
        final client = ReceiptApiClient(
          baseUrl: baseUrl,
          httpClient: MockClient((request) async {
            expect(request.method, 'POST');
            expect(request.url, baseUrl.resolve('/api/receipts/pdf'));
            expect(
              request.headers['content-type'],
              contains('application/json'),
            );
            expect(request.body, receiptJson);

            return http.Response.bytes(
              expectedBytes,
              200,
              headers: {'content-type': 'application/pdf'},
            );
          }),
        );

        final bytes = await client.generatePdf(receiptJson);

        expect(bytes, isA<Uint8List>());
        expect(bytes, expectedBytes);
      },
    );

    test(
      'generatePdf throws ReceiptApiException with validation errors',
      () async {
        final client = ReceiptApiClient(
          baseUrl: baseUrl,
          httpClient: MockClient((request) async {
            expect(request.method, 'POST');
            expect(request.url, baseUrl.resolve('/api/receipts/pdf'));

            return http.Response(
              jsonEncode({
                'title': 'Receipt validation failed.',
                'status': 400,
                'detail': 'Receipt validation failed with 1 error(s).',
                'errors': [
                  {
                    'field': 'business.businessName',
                    'message': 'Business name is required.',
                  },
                ],
              }),
              400,
              headers: {'content-type': 'application/problem+json'},
            );
          }),
        );

        await expectLater(
          client.generatePdf(receiptJson),
          throwsA(
            isA<ReceiptApiException>()
                .having((error) => error.statusCode, 'statusCode', 400)
                .having((error) => error.errors, 'errors', hasLength(1))
                .having(
                  (error) => error.errors.single.field,
                  'field',
                  'business.businessName',
                ),
          ),
        );
      },
    );

    test('generatePng throws ReceiptApiException for server errors', () async {
      final client = ReceiptApiClient(
        baseUrl: baseUrl,
        httpClient: MockClient((request) async {
          expect(request.method, 'POST');
          expect(request.url, baseUrl.resolve('/api/receipts/png'));

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
      );

      await expectLater(
        client.generatePng(receiptJson),
        throwsA(
          isA<ReceiptApiException>()
              .having((error) => error.statusCode, 'statusCode', 500)
              .having(
                (error) => error.message,
                'message',
                contains('Receipt generation failed.'),
              ),
        ),
      );
    });
  });

  test('close does not close an injected HTTP client', () {
    final httpClient = _RecordingClient();
    final client = ReceiptApiClient(baseUrl: baseUrl, httpClient: httpClient);

    client.close();

    expect(httpClient.closed, isFalse);
  });

  final timeoutCases = <String, Future<Object?> Function(ReceiptApiClient)>{
    'validate': (client) => client.validate(receiptJson),
    'generatePng': (client) => client.generatePng(receiptJson),
    'generatePdf': (client) => client.generatePdf(receiptJson),
  };

  for (final timeoutCase in timeoutCases.entries) {
    test(
      '${timeoutCase.key} times out instead of waiting indefinitely',
      () async {
        final client = ReceiptApiClient(
          baseUrl: baseUrl,
          requestTimeout: const Duration(milliseconds: 1),
          httpClient: MockClient(
            (request) => Completer<http.Response>().future,
          ),
        );

        await expectLater(
          timeoutCase.value(client),
          throwsA(isA<TimeoutException>()),
        );
      },
    );
  }
}

class _RecordingClient extends http.BaseClient {
  bool closed = false;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    return http.StreamedResponse(const Stream<List<int>>.empty(), 200);
  }

  @override
  void close() {
    closed = true;
    super.close();
  }
}
