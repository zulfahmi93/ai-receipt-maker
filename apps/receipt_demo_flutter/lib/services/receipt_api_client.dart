import 'dart:convert';
import 'dart:typed_data';

import 'package:http/http.dart' as http;

class ReceiptValidationError {
  const ReceiptValidationError({required this.field, required this.message});

  final String field;
  final String message;
}

class ReceiptValidationResult {
  const ReceiptValidationResult({required this.valid, required this.errors});

  final bool valid;
  final List<ReceiptValidationError> errors;
}

class ReceiptApiException implements Exception {
  const ReceiptApiException({
    required this.statusCode,
    required this.message,
    this.errors = const <ReceiptValidationError>[],
  });

  final int statusCode;
  final String message;
  final List<ReceiptValidationError> errors;

  @override
  String toString() => 'ReceiptApiException($statusCode): $message';
}

class ReceiptApiClient {
  ReceiptApiClient({
    required this.baseUrl,
    http.Client? httpClient,
    this.requestTimeout = defaultRequestTimeout,
  }) : _httpClient = httpClient ?? http.Client(),
       _ownsHttpClient = httpClient == null;

  static const Duration defaultRequestTimeout = Duration(seconds: 30);

  final Uri baseUrl;
  final Duration requestTimeout;
  final http.Client _httpClient;
  final bool _ownsHttpClient;

  Future<ReceiptValidationResult> validate(String receiptJson) async {
    final response = await _postJson('/api/receipts/validate', receiptJson);
    final body = jsonDecode(response.body) as Map<String, dynamic>;

    return ReceiptValidationResult(
      valid: body['valid'] as bool? ?? false,
      errors: _parseValidationErrors(body['errors']),
    );
  }

  Future<Uint8List> generatePng(String receiptJson) async {
    final response = await _postJson('/api/receipts/png', receiptJson);
    return response.bodyBytes;
  }

  Future<Uint8List> generatePdf(String receiptJson) async {
    final response = await _postJson('/api/receipts/pdf', receiptJson);
    return response.bodyBytes;
  }

  Future<http.Response> _postJson(String path, String receiptJson) {
    return _httpClient
        .post(
          baseUrl.resolve(path),
          headers: const {'content-type': 'application/json'},
          body: receiptJson,
        )
        .timeout(requestTimeout)
        .then(_ensureSuccess);
  }

  void close() {
    if (_ownsHttpClient) {
      _httpClient.close();
    }
  }

  static List<ReceiptValidationError> _parseValidationErrors(Object? errors) {
    if (errors is! List) {
      return const <ReceiptValidationError>[];
    }

    return errors
        .whereType<Map<String, dynamic>>()
        .map(
          (error) => ReceiptValidationError(
            field: error['field'] as String? ?? '',
            message: error['message'] as String? ?? '',
          ),
        )
        .toList();
  }

  static http.Response _ensureSuccess(http.Response response) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return response;
    }

    throw _parseApiException(response);
  }

  static ReceiptApiException _parseApiException(http.Response response) {
    try {
      final body = jsonDecode(response.body);
      if (body is Map<String, dynamic>) {
        final title = body['title'] as String?;
        final detail = body['detail'] as String?;
        return ReceiptApiException(
          statusCode: response.statusCode,
          message: _joinMessage(title, detail, response.reasonPhrase),
          errors: _parseValidationErrors(body['errors']),
        );
      }
    } on FormatException {
      // Fall through to the transport-level message below.
    }

    return ReceiptApiException(
      statusCode: response.statusCode,
      message: response.reasonPhrase ?? 'HTTP ${response.statusCode}',
    );
  }

  static String _joinMessage(String? title, String? detail, String? fallback) {
    final parts = [
      if (title != null && title.trim().isNotEmpty) title.trim(),
      if (detail != null && detail.trim().isNotEmpty) detail.trim(),
    ];

    if (parts.isEmpty) {
      return fallback ?? 'Receipt API request failed.';
    }

    return parts.join(' ');
  }
}
