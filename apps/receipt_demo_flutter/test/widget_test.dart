import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:receipt_demo_flutter/main.dart';

void main() {
  testWidgets('Receipt Toolkit app shell exposes the core work surfaces', (
    WidgetTester tester,
  ) async {
    await tester.pumpWidget(const MyApp());
    await tester.pumpAndSettle();

    expect(find.text('Receipt Toolkit'), findsWidgets);
    expect(
      find.byKey(const Key('receipt-preview-placeholder')),
      findsOneWidget,
    );
    expect(find.byKey(const Key('json-editor-validate')), findsOneWidget);
    expect(find.byKey(const Key('theme-color-accentColor')), findsOneWidget);
    expect(find.byKey(const Key('share-pdf')), findsOneWidget);
    expect(find.byKey(const Key('share-png')), findsOneWidget);
    expect(find.byKey(const Key('reset-sample')), findsOneWidget);
  });
}
