import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../services/receipt_api_client.dart';
import '../state/receipt_state.dart';

class JsonEditorScreen extends StatefulWidget {
  const JsonEditorScreen({super.key});

  @override
  State<JsonEditorScreen> createState() => _JsonEditorScreenState();
}

class _JsonEditorScreenState extends State<JsonEditorScreen> {
  late final TextEditingController _controller;
  ReceiptState? _state;
  List<ReceiptValidationError> _errors = const <ReceiptValidationError>[];

  @override
  void initState() {
    super.initState();
    _controller = TextEditingController();
  }

  @override
  void dispose() {
    _state?.removeListener(_syncFromState);
    _controller.dispose();
    super.dispose();
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final nextState = context.read<ReceiptState>();
    if (identical(_state, nextState)) {
      return;
    }

    _state?.removeListener(_syncFromState);
    _state = nextState;
    _syncController(nextState.jsonText);
    nextState.addListener(_syncFromState);
  }

  @override
  Widget build(BuildContext context) {
    final state = _state ?? context.read<ReceiptState>();

    return Column(
      children: [
        TextField(
          controller: _controller,
          maxLines: null,
          onChanged: state.setJsonText,
        ),
        ElevatedButton(
          key: const Key('json-editor-validate'),
          onPressed: () => _validate(state),
          child: const Text('Validate'),
        ),
        if (_errors.isNotEmpty)
          Column(
            key: const Key('json-editor-errors'),
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              for (final error in _errors)
                Text('${error.field}: ${error.message}'),
            ],
          ),
      ],
    );
  }

  Future<void> _validate(ReceiptState state) async {
    try {
      jsonDecode(state.jsonText);
    } on FormatException catch (error) {
      setState(() {
        _errors = [
          ReceiptValidationError(
            field: _formatJsonErrorLocation(error),
            message: 'JSON ${error.message}',
          ),
        ];
      });
      return;
    }

    final result = await state.validate();
    if (!mounted) {
      return;
    }

    setState(() {
      _errors = result.errors;
    });
  }

  void _syncController(String jsonText) {
    if (_controller.text == jsonText) {
      return;
    }

    _controller.value = TextEditingValue(
      text: jsonText,
      selection: TextSelection.collapsed(offset: jsonText.length),
    );
  }

  void _syncFromState() {
    final state = _state;
    if (state != null) {
      _syncController(state.jsonText);
    }
  }

  static String _formatJsonErrorLocation(FormatException error) {
    final offset = error.offset;
    final source = error.source?.toString();
    final firstKey = source == null
        ? null
        : RegExp(r'"([^"]+)"').firstMatch(source)?.group(1);

    final location = firstKey ?? 'JSON input';
    if (offset == null) {
      return location;
    }

    return '$location at offset $offset';
  }
}
