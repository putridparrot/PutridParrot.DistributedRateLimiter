{{- define "drl.name" -}}
{{- .Values.app.name | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{- define "drl.namespace" -}}
{{- if .Values.namespaceOverride -}}
{{- .Values.namespaceOverride -}}
{{- else -}}
{{- .Release.Namespace -}}
{{- end -}}
{{- end -}}
