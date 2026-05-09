import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../state/receipt_state.dart';

class ThemePanel extends StatelessWidget {
  const ThemePanel({super.key});

  @override
  Widget build(BuildContext context) {
    final state = context.watch<ReceiptState>();
    final json = _decode(state.jsonText);
    final theme = _object(json['theme']);
    final options = _object(json['options']);
    final layout = _object(json['layout']);
    final width = layout['receiptWidth'] as int? ?? 360;

    return ListView(
      children: [
        _colorField(state, 'accentColor', theme['accentColor'] as String?),
        _colorField(
          state,
          'highlightColor',
          theme['highlightColor'] as String?,
        ),
        _colorField(state, 'paperColor', theme['paperColor'] as String?),
        _colorField(state, 'textColor', theme['textColor'] as String?),
        Switch(
          key: const Key('theme-toggle-qr'),
          value: options['showQrCode'] as bool? ?? true,
          onChanged: state.toggleQr,
        ),
        Switch(
          key: const Key('theme-toggle-logo'),
          value: options['showLogo'] as bool? ?? true,
          onChanged: state.toggleLogo,
        ),
        Switch(
          key: const Key('theme-toggle-footer-contact'),
          value: options['showFooterContact'] as bool? ?? true,
          onChanged: state.toggleFooterContact,
        ),
        DropdownButton<int>(
          key: const Key('theme-width-selector'),
          value: width,
          items: const [
            DropdownMenuItem(value: 360, child: Text('360')),
            DropdownMenuItem(value: 420, child: Text('420')),
          ],
          onChanged: (value) {
            if (value != null) {
              state.updateReceiptWidth(value);
            }
          },
        ),
      ],
    );
  }

  static Widget _colorField(ReceiptState state, String key, String? value) {
    return _ThemeColorField(
      fieldKey: key,
      value: value ?? '',
      onSubmitted: (hex) => state.updateThemeColor(key, hex),
    );
  }

  static Map<String, dynamic> _decode(String jsonText) {
    if (jsonText.trim().isEmpty) {
      return <String, dynamic>{};
    }

    try {
      final value = jsonDecode(jsonText);
      if (value is Map<String, dynamic>) {
        return value;
      }
    } on FormatException {
      return <String, dynamic>{};
    }

    return <String, dynamic>{};
  }

  static Map<String, dynamic> _object(Object? value) {
    if (value is Map<String, dynamic>) {
      return value;
    }

    return <String, dynamic>{};
  }
}

class _ThemeColorField extends StatefulWidget {
  const _ThemeColorField({
    required this.fieldKey,
    required this.value,
    required this.onSubmitted,
  });

  final String fieldKey;
  final String value;
  final ValueChanged<String> onSubmitted;

  @override
  State<_ThemeColorField> createState() => _ThemeColorFieldState();
}

class _ThemeColorFieldState extends State<_ThemeColorField> {
  late final TextEditingController _controller;

  @override
  void initState() {
    super.initState();
    _controller = TextEditingController(text: widget.value);
  }

  @override
  void didUpdateWidget(covariant _ThemeColorField oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.value != widget.value && _controller.text != widget.value) {
      _controller.value = TextEditingValue(
        text: widget.value,
        selection: TextSelection.collapsed(offset: widget.value.length),
      );
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return TextField(
      key: Key('theme-color-${widget.fieldKey}'),
      controller: _controller,
      onSubmitted: widget.onSubmitted,
      textInputAction: TextInputAction.done,
    );
  }
}
