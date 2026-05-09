import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:provider/provider.dart';
import 'package:receipt_demo_flutter/services/receipt_api_client.dart';
import 'package:receipt_demo_flutter/state/receipt_state.dart';
import 'package:receipt_demo_flutter/widgets/theme_panel.dart';

void main() {
  final baseUrl = Uri.parse('https://receipt-toolkit.test');

  group('ThemePanel', () {
    testWidgets(
      'T7.11 exposes four color controls, three toggles, and a width selector',
      (tester) async {
        final state = _state(baseUrl);

        await tester.pumpWidget(_hostedPanel(state));

        expect(
          find.byKey(const Key('theme-color-accentColor')),
          findsOneWidget,
        );
        expect(
          find.byKey(const Key('theme-color-highlightColor')),
          findsOneWidget,
        );
        expect(find.byKey(const Key('theme-color-paperColor')), findsOneWidget);
        expect(find.byKey(const Key('theme-color-textColor')), findsOneWidget);
        expect(find.byKey(const Key('theme-toggle-qr')), findsOneWidget);
        expect(find.byKey(const Key('theme-toggle-logo')), findsOneWidget);
        expect(
          find.byKey(const Key('theme-toggle-footer-contact')),
          findsOneWidget,
        );
        expect(find.byKey(const Key('theme-width-selector')), findsOneWidget);
      },
    );

    testWidgets('T7.11 changing accent color updates ReceiptState JSON', (
      tester,
    ) async {
      final state = _state(baseUrl);

      await tester.pumpWidget(_hostedPanel(state));
      await tester.enterText(
        find.byKey(const Key('theme-color-accentColor')),
        '#C83232',
      );
      await tester.testTextInput.receiveAction(TextInputAction.done);
      await tester.pump();

      final json = _decodeReceiptJson(state.jsonText);
      expect(json['theme'], containsPair('accentColor', '#C83232'));
    });

    testWidgets(
      'T7.11 toggling QR updates options.showQrCode in ReceiptState JSON',
      (tester) async {
        final state = _state(baseUrl);

        await tester.pumpWidget(_hostedPanel(state));
        await tester.tap(find.byKey(const Key('theme-toggle-qr')));
        await tester.pump();

        final json = _decodeReceiptJson(state.jsonText);
        expect(json['options'], containsPair('showQrCode', false));
      },
    );

    testWidgets(
      'T7.11 width selector updates layout.receiptWidth in ReceiptState JSON',
      (tester) async {
        final state = _state(baseUrl);

        await tester.pumpWidget(_hostedPanel(state));
        await tester.tap(find.byKey(const Key('theme-width-selector')));
        await tester.pumpAndSettle();
        await tester.tap(find.text('420').last);
        await tester.pumpAndSettle();

        final json = _decodeReceiptJson(state.jsonText);
        expect(json['layout'], containsPair('receiptWidth', 420));
      },
    );

    testWidgets('updates color fields after external JSON changes', (
      tester,
    ) async {
      final state = _state(baseUrl);

      await tester.pumpWidget(_hostedPanel(state));
      expect(_accentColorField(tester).controller?.text, '#3F6F63');

      state.setJsonText(
        jsonEncode({
          'schemaVersion': 1,
          'theme': {
            'accentColor': '#C83232',
            'highlightColor': '#F0E8E8',
            'paperColor': '#FAFAFA',
            'textColor': '#101010',
          },
          'options': {
            'showQrCode': false,
            'showLogo': true,
            'showFooterContact': true,
          },
          'layout': {'receiptWidth': 420},
        }),
      );
      await tester.pump();

      expect(_accentColorField(tester).controller?.text, '#C83232');
    });
  });
}

TextField _accentColorField(WidgetTester tester) {
  return tester.widget<TextField>(
    find.byKey(const Key('theme-color-accentColor')),
  );
}

ReceiptState _state(Uri baseUrl) {
  return ReceiptState(
    apiClient: ReceiptApiClient(
      baseUrl: baseUrl,
      httpClient: MockClient((_) async => http.Response('', 500)),
    ),
    initialJsonText: jsonEncode({
      'schemaVersion': 1,
      'theme': {
        'accentColor': '#3F6F63',
        'highlightColor': '#E8F0EC',
        'paperColor': '#FFFFFF',
        'textColor': '#202124',
      },
      'options': {
        'showQrCode': true,
        'showLogo': true,
        'showFooterContact': true,
      },
      'layout': {'receiptWidth': 360},
    }),
  );
}

Widget _hostedPanel(ReceiptState state) {
  return ChangeNotifierProvider<ReceiptState>.value(
    value: state,
    child: const MaterialApp(home: Scaffold(body: ThemePanel())),
  );
}

Map<String, dynamic> _decodeReceiptJson(String receiptJson) {
  return jsonDecode(receiptJson) as Map<String, dynamic>;
}
