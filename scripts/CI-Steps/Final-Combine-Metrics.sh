     set -eu; set -o pipefail
     Say "SYSTEM_ARTIFACTSDIRECTORY: [$SYSTEM_ARTIFACTSDIRECTORY]"

     Build-Combined-Report() {
       local pattern_file="$1"
       local combined_report_file="$2"
       rm -f "$combined_report_file"

       Say "BUILDING COMBINED REPORT '$combined_report_file' using pattern [$pattern_file]"

       find "${SYSTEM_ARTIFACTSDIRECTORY}" -name "$pattern_file" | sort | while IFS='' read -r file; do
           report_folder="$(dirname "$file")"
           job_name="$(basename "$report_folder")"
           Say "SUMMARY for $job_name, metrics is '$file'"
           summary_file=$(mktemp)
           for info_file in JOB-NAME.TXT CPU-NAME.TXT MEMORY-INFO.TXT OS-NAME.TXT SQL-SERVER-MEDIUM-VERSION.TXT SQL-SERVER-TITLE.TXT; do
             info_key="${info_file%.*}"
             echo "KEY: [$info_key]"
             value_full_name="$report_folder/$info_file"
             echo "value_full_name: [$value_full_name]"
             # ON Linux these files are missing
             value="$(cat "$value_full_name" || true)"
             echo "KEY: [$info_key] VALUE: [$value]"
             Colorize Green "$(echo "$info_key: $value" | tee -a "$summary_file")"
           done
           sed -i 's/\r$//' "$file" # dos2unix
           (echo $job_name; cat "$summary_file" || true; awk -v RS= 'NR==1' "$file"; echo "") | tee -a "$combined_report_file"
           Colorize Green "Done: Added job metrics '$file' for combined report '$combined_report_file'"
       done
     }

     Build-Combined-Report "AddAction.log" "$SYSTEM_ARTIFACTSDIRECTORY/Benchmark-AddAction.txt"
     Build-Combined-Report "SqlInsights Report.txt" "$SYSTEM_ARTIFACTSDIRECTORY/Benchmark-W3API-Report.txt"
