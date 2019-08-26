using UnityEngine;

public struct Name {
    public string fullName;
    public string shortform;
}

public class NameSingleton
{
    private static NameSingleton backingInstance;

    public static NameSingleton sharedInstance {
        get {
            if (backingInstance == null) {
                backingInstance = new NameSingleton();
            }

            return backingInstance;
        }
    }

    class NameList {
        public string[] names;
    }

    System.Random randomGenerator;
    NameList maleNames;
    NameList femaleNames;
    NameList lastNames;

    private bool lastNameMale = false;

    private NameSingleton() {
        
        TextAsset maleJson = Resources.Load<TextAsset>("JSON/firstnames_m");
        TextAsset femaleJson = Resources.Load<TextAsset>("JSON/firstnames_f");
        TextAsset surnameJson = Resources.Load<TextAsset>("JSON/surnames");

        try {
            maleNames = JsonUtility.FromJson<NameList>(maleJson.text);
            femaleNames = JsonUtility.FromJson<NameList>(femaleJson.text);
            lastNames = JsonUtility.FromJson<NameList>(surnameJson.text);
        } catch {
            maleNames = new NameList();
            femaleNames = new NameList();
            lastNames = new NameList();

            maleNames.names = new string[] { "John" };
            femaleNames.names = new string[] { "Jill" };
            lastNames.names = new string[] { "Johnson" };
        }

        randomGenerator = new System.Random(System.Guid.NewGuid().GetHashCode());
    }

    public Name GenerateName() {

        string firstName = "";

        if (lastNameMale) {
            firstName = maleNames.names[randomGenerator.Next(0, maleNames.names.Length)];
        } else {
            firstName = femaleNames.names[randomGenerator.Next(0, femaleNames.names.Length)];
        }

        lastNameMale = !lastNameMale;

        string lastName = lastNames.names[randomGenerator.Next(0, lastNames.names.Length)];

        Name name = new Name();
        name.fullName = firstName + " " + lastName;
        name.shortform = firstName + " " + lastName.ToCharArray()[0] + ".";

        return name;
    }
}
