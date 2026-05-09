import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:provider/provider.dart';
import 'package:receipt_demo_flutter/services/receipt_api_client.dart';
import 'package:receipt_demo_flutter/state/receipt_state.dart';
import 'package:receipt_demo_flutter/widgets/json_editor_screen.dart';

void main() {
  final baseUrl = Uri.parse('https://receipt-toolkit.test');

  group('JsonEditorScreen', () {
    testWidgets(
      'T7.9 malformed JSON shows parse-time errors without calling validation API',
      (tester) async {
        var apiWasCalled = false;
        final state = ReceiptState(
          apiClient: ReceiptApiClient(
            baseUrl: baseUrl,
            httpClient: MockClient((_) async {
              apiWasCalled = true;
              return http.Response(
                jsonEncode({'valid': true, 'errors': <Object>[]}),
                200,
              );
            }),
          ),
          initialJsonText: '{"schemaVersion":1',
        );

        await tester.pumpWidget(_hostedEditor(state));
        await tester.tap(find.byKey(const Key('json-editor-validate')));
        await tester.pumpAndSettle();

        expect(apiWasCalled, isFalse);
        expect(find.byKey(const Key('json-editor-errors')), findsOneWidget);
        expect(find.textContaining('JSON'), findsWidgets);
        expect(find.textContaining('schemaVersion'), findsWidgets);
        expect(
          find.descendant(
            of: find.byKey(const Key('json-editor-errors')),
            matching: find.textContaining(state.jsonText),
          ),
          findsNothing,
        );
      },
    );

    testWidgets('T7.10 API validation errors render with field paths', (
      tester,
    ) async {
      const receiptJson = '{"schemaVersion":1,"business":{}}';
      final state = ReceiptState(
        apiClient: ReceiptApiClient(
          baseUrl: baseUrl,
          httpClient: MockClient((request) async {
            expect(request.method, 'POST');
            expect(request.url, baseUrl.resolve('/api/receipts/validate'));
            expect(request.body, receiptJson);

            return http.Response(
              jsonEncode({
                'valid': false,
                'errors': [
                  {
                    'field': 'business.businessName',
                    'message': 'Business name is required.',
                  },
                  {
                    'field': 'items[0].unitPrice',
                    'message': 'Unit price must be zero or greater.',
                  },
                ],
              }),
              200,
              headers: {'content-type': 'application/json; charset=utf-8'},
            );
          }),
        ),
        initialJsonText: receiptJson,
      );

      await tester.pumpWidget(_hostedEditor(state));
      await tester.tap(find.byKey(const Key('json-editor-validate')));
      await tester.pumpAndSettle();

      expect(find.byKey(const Key('json-editor-errors')), findsOneWidget);
      expect(find.textContaining('business.businessName'), findsOneWidget);
      expect(find.textContaining('Business name is required.'), findsOneWidget);
      expect(find.textContaining('items[0].unitPrice'), findsOneWidget);
      expect(
        find.textContaining('Unit price must be zero or greater.'),
        findsOneWidget,
      );
    });
  });
}

Widget _hostedEditor(ReceiptState state) {
  return ChangeNotifierProvider<ReceiptState>.value(
    value: state,
    child: const MaterialApp(home: Scaffold(body: JsonEditorScreen())),
  );
}
