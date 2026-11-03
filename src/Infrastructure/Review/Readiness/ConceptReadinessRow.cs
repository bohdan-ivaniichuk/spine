using Spine.Application.Review;
using Microsoft.EntityFrameworkCore;

namespace Spine.Infrastructure.Review.Readiness;

/// <summary>Readiness counts of one concept's schedulable questions.</summary>
internal sealed record ConceptReadinessRow(int ConceptId, int Questions, int Stated, int Ready);
