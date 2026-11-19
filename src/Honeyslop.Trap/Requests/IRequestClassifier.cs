namespace Honeyslop.Trap.Requests;

public interface IRequestClassifier
{
    Classification Classify(string method, string target);
}