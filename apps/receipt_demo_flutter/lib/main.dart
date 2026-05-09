import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'services/receipt_api_client.dart';
import 'services/receipt_share_service.dart';
import 'state/receipt_state.dart';
import 'widgets/json_editor_screen.dart';
import 'widgets/receipt_actions.dart';
import 'widgets/receipt_preview.dart';
import 'widgets/theme_panel.dart';

void main() {
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  static final Uri _defaultBaseUrl = Uri.parse(
    const String.fromEnvironment(
      'API_BASE_URL',
      defaultValue: 'http://localhost:5273',
    ),
  );

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<ReceiptState>(
      create: (_) {
        final state = ReceiptState(
          apiClient: ReceiptApiClient(baseUrl: _defaultBaseUrl),
        );
        unawaited(state.loadSample());
        return state;
      },
      child: MaterialApp(
        title: 'Receipt Toolkit',
        theme: ThemeData(
          colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF3F6F63)),
        ),
        home: const ReceiptToolkitShell(),
      ),
    );
  }
}

class ReceiptToolkitShell extends StatelessWidget {
  const ReceiptToolkitShell({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Receipt Toolkit')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Expanded(
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Expanded(
                    child: Center(
                      child: Consumer<ReceiptState>(
                        builder: (context, state, _) {
                          return ReceiptPreview(
                            previewBytes: state.previewBytes,
                          );
                        },
                      ),
                    ),
                  ),
                  const SizedBox(width: 16),
                  const Expanded(
                    child: SingleChildScrollView(child: JsonEditorScreen()),
                  ),
                  const SizedBox(width: 16),
                  const SizedBox(width: 240, child: ThemePanel()),
                ],
              ),
            ),
            const SizedBox(height: 16),
            const ReceiptActions(shareService: DefaultReceiptShareService()),
          ],
        ),
      ),
    );
  }
}
