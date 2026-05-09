import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';

import '../services/receipt_api_client.dart';

class ReceiptState extends ChangeNotifier {
  ReceiptState({required ReceiptApiClient apiClient, String? initialJsonText})
    : _apiClient = apiClient,
      _jsonText = initialJsonText ?? '';

  final ReceiptApiClient _apiClient;
  String _jsonText;
  Uint8List? _previewBytes;
  bool _isLoading = false;
  String? _errorMessage;

  String get jsonText => _jsonText;

  Uint8List? get previewBytes => _previewBytes;

  bool get isLoading => _isLoading;

  String? get errorMessage => _errorMessage;

  void setJsonText(String value) {
    _jsonText = value;
    notifyListeners();
  }

  Future<ReceiptValidationResult> validate() {
    return _apiClient.validate(_jsonText);
  }

  Future<void> loadSample() async {
    _jsonText = await rootBundle.loadString('assets/sample_receipt_data.json');
    notifyListeners();
  }

  Future<void> regenerate() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      _previewBytes = await _apiClient.generatePng(_jsonText);
    } on Object catch (error) {
      _previewBytes = null;
      _errorMessage = _friendlyErrorMessage(error);
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<Uint8List> generatePdf() {
    return _apiClient.generatePdf(_jsonText);
  }

  @override
  void dispose() {
    _apiClient.close();
    super.dispose();
  }

  void updateAccentColor(String hex) {
    updateThemeColor('accentColor', hex);
  }

  void updateThemeColor(String key, String hex) {
    final json = _decodeJsonObject();
    final theme = _objectProperty(json, 'theme');
    theme[key] = hex;
    json['theme'] = theme;
    _jsonText = jsonEncode(json);
    notifyListeners();
  }

  void toggleQr(bool enabled) {
    final json = _decodeJsonObject();
    final options = _objectProperty(json, 'options');
    options['showQrCode'] = enabled;
    json['options'] = options;
    _jsonText = jsonEncode(json);
    notifyListeners();
  }

  void toggleLogo(bool enabled) {
    final json = _decodeJsonObject();
    final options = _objectProperty(json, 'options');
    options['showLogo'] = enabled;
    json['options'] = options;
    _jsonText = jsonEncode(json);
    notifyListeners();
  }

  void toggleFooterContact(bool enabled) {
    final json = _decodeJsonObject();
    final options = _objectProperty(json, 'options');
    options['showFooterContact'] = enabled;
    json['options'] = options;
    _jsonText = jsonEncode(json);
    notifyListeners();
  }

  void updateReceiptWidth(int width) {
    final json = _decodeJsonObject();
    final layout = _objectProperty(json, 'layout');
    layout['receiptWidth'] = width;
    json['layout'] = layout;
    _jsonText = jsonEncode(json);
    notifyListeners();
  }

  Map<String, dynamic> _decodeJsonObject() {
    if (_jsonText.trim().isEmpty) {
      throw StateError('Receipt JSON must be loaded before editing fields.');
    }

    final value = jsonDecode(_jsonText);
    if (value is Map<String, dynamic>) {
      return value;
    }

    throw StateError('Receipt JSON must be a top-level object.');
  }

  static Map<String, dynamic> _objectProperty(
    Map<String, dynamic> json,
    String key,
  ) {
    // Missing section objects are materialized only after the receipt root exists.
    final value = json[key];
    if (value is Map<String, dynamic>) {
      return value;
    }

    return <String, dynamic>{};
  }

  static String _friendlyErrorMessage(Object error) {
    if (error is ReceiptApiException) {
      return error.message;
    }

    return error.toString();
  }
}
