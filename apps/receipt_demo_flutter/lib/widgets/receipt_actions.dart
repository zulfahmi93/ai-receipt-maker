import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../services/receipt_share_service.dart';
import '../state/receipt_state.dart';

class ReceiptActions extends StatefulWidget {
  const ReceiptActions({super.key, required this.shareService});

  final ReceiptShareService shareService;

  @override
  State<ReceiptActions> createState() => _ReceiptActionsState();
}

class _ReceiptActionsState extends State<ReceiptActions> {
  bool _busy = false;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        FilledButton(
          key: const Key('share-pdf'),
          onPressed: _busy ? null : () => _run(_sharePdf),
          child: const Text('Share PDF'),
        ),
        FilledButton.tonal(
          key: const Key('share-png'),
          onPressed: _busy ? null : () => _run(_sharePng),
          child: const Text('Share PNG'),
        ),
        OutlinedButton(
          key: const Key('reset-sample'),
          onPressed: _busy ? null : () => _run(_resetSample),
          child: const Text('Reset Sample'),
        ),
      ],
    );
  }

  Future<void> _run(Future<void> Function() action) async {
    setState(() {
      _busy = true;
    });

    try {
      await action();
    } finally {
      if (mounted) {
        setState(() {
          _busy = false;
        });
      }
    }
  }

  Future<void> _sharePdf() async {
    final state = context.read<ReceiptState>();
    final bytes = await state.generatePdf();
    if (!mounted) {
      return;
    }

    await widget.shareService.sharePdf(bytes);
    if (!mounted) {
      return;
    }
  }

  Future<void> _sharePng() async {
    final state = context.read<ReceiptState>();
    var bytes = state.previewBytes;
    if (bytes == null) {
      await state.regenerate();
      if (!mounted) {
        return;
      }
      bytes = state.previewBytes;
    }

    if (bytes != null) {
      await widget.shareService.sharePng(bytes);
      if (!mounted) {
        return;
      }
    }
  }

  Future<void> _resetSample() async {
    final state = context.read<ReceiptState>();
    await state.loadSample();
    if (!mounted) {
      return;
    }

    await state.regenerate();
  }
}
