/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#nullable enable

using System.Threading.Tasks;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Status of a service validation check.
    /// </summary>
    public enum ValidationStatus
    {
        /// <summary>Validation has not been performed yet.</summary>
        Unknown,
        /// <summary>Validation is currently in progress.</summary>
        Validating,
        /// <summary>Validation passed - service is properly configured.</summary>
        Valid,
        /// <summary>Validation failed - service has configuration issues.</summary>
        Invalid,
        /// <summary>Validation encountered an error during the check.</summary>
        Error
    }

    /// <summary>
    /// Result of a service validation check.
    /// </summary>
    public struct ValidationResult
    {
        /// <summary>The validation status.</summary>
        public ValidationStatus Status;

        /// <summary>Human-readable message describing the validation result.</summary>
        public string Message;

        /// <summary>
        /// Creates a new validation result.
        /// </summary>
        public ValidationResult(ValidationStatus status, string message)
        {
            Status = status;
            Message = message;
        }

        /// <summary>Creates a successful validation result.</summary>
        public static ValidationResult Valid(string message = "Configuration is valid")
            => new ValidationResult(ValidationStatus.Valid, message);

        /// <summary>Creates a failed validation result.</summary>
        public static ValidationResult Invalid(string message)
            => new ValidationResult(ValidationStatus.Invalid, message);

        /// <summary>Creates an error validation result.</summary>
        public static ValidationResult Error(string message)
            => new ValidationResult(ValidationStatus.Error, message);

        /// <summary>Creates an unknown validation result.</summary>
        public static ValidationResult Unknown()
            => new ValidationResult(ValidationStatus.Unknown, "Validation not performed");

        /// <summary>Creates a validating-in-progress result.</summary>
        public static ValidationResult Validating()
            => new ValidationResult(ValidationStatus.Validating, "Validating...");
    }

    /// <summary>
    /// Interface for AI services that support configuration validation.
    /// Validation should be performed without consuming AI inference tokens.
    /// </summary>
    public interface IServiceValidation
    {
        /// <summary>
        /// Gets the current validation result (cached from last validation).
        /// </summary>
        ValidationResult CurrentValidationResult { get; }

        /// <summary>
        /// Validates the service configuration asynchronously.
        /// This should NOT consume any AI inference tokens - only check connectivity/availability.
        /// </summary>
        /// <returns>The validation result.</returns>
        Task<ValidationResult> ValidateConfigurationAsync();
    }
}
